using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.CallManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Services
{
    public class AttachmentService : IAttachmentService
    {
        private static readonly Dictionary<string, string[]> AllowedContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = new[] { "image/png" },
            [".jpg"] = new[] { "image/jpeg" },
            [".jpeg"] = new[] { "image/jpeg" },
            [".pdf"] = new[] { "application/pdf" },
            [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/octet-stream" },
            [".txt"] = new[] { "text/plain" },
            [".log"] = new[] { "text/plain", "application/octet-stream" }
        };

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration, ILogger<AttachmentService> logger)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        // Section 23: "Store actual files in configurable storage" — FileUpload:UploadPath in
        // appsettings.json, resolved relative to the content root so it isn't tied to wwwroot.
        private string ResolveUploadRoot(int callLogId)
        {
            var configuredPath = _configuration["FileUpload:UploadPath"] ?? "wwwroot/uploads";
            var basePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(_environment.ContentRootPath, configuredPath);

            return Path.Combine(basePath, callLogId.ToString());
        }

        public async Task<List<CallAttachment>> SaveAsync(int callLogId, IReadOnlyList<IFormFile>? files, string uploadedByUserId)
        {
            var results = new List<CallAttachment>();
            if (files == null || files.Count == 0)
            {
                return results;
            }

            var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>()
                ?? AllowedContentTypesByExtension.Keys.ToArray();
            var maxSizeBytes = (_configuration.GetValue<int?>("FileUpload:MaxFileSizeMB") ?? 10) * 1024L * 1024L;

            var uploadRoot = ResolveUploadRoot(callLogId);
            Directory.CreateDirectory(uploadRoot);

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    continue;
                }

                var extension = Path.GetExtension(file.FileName);

                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"File type \"{extension}\" is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}.");
                }

                if (file.Length > maxSizeBytes)
                {
                    throw new InvalidOperationException($"File \"{file.FileName}\" exceeds the {maxSizeBytes / 1024 / 1024} MB size limit.");
                }

                if (AllowedContentTypesByExtension.TryGetValue(extension, out var expectedContentTypes) &&
                    !string.IsNullOrEmpty(file.ContentType) &&
                    !expectedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Attachment {FileName} declared ContentType {ContentType} not in expected set for {Extension}", file.FileName, file.ContentType, extension);
                    throw new InvalidOperationException($"File \"{file.FileName}\" does not appear to be a valid {extension} file.");
                }

                // Never trust the client filename for the on-disk path — store under a generated
                // name; the original (sanitized) name is kept only as display metadata.
                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(uploadRoot, storedFileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                results.Add(new CallAttachment
                {
                    CallLogId = callLogId,
                    FileName = Path.GetFileName(file.FileName),
                    FilePath = $"/uploads/{callLogId}/{storedFileName}",
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedById = uploadedByUserId,
                    UploadedDate = DateTime.Now
                });
            }

            return results;
        }

        public async Task<AttachmentDeleteResult> DeleteAsync(int attachmentId)
        {
            var attachment = await _context.CallAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId);
            if (attachment == null)
            {
                return AttachmentDeleteResult.Failure("Attachment not found.");
            }

            // FilePath is stored as a web-relative URL (/uploads/{callLogId}/{file}); resolve it
            // back to the same configurable root used at upload time to physically remove it.
            var relativeSegments = attachment.FilePath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var configuredPath = _configuration["FileUpload:UploadPath"] ?? "wwwroot/uploads";
            var basePath = Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(_environment.ContentRootPath, configuredPath);
            var physicalPath = Path.Combine(new[] { basePath }.Concat(relativeSegments.Skip(1)).ToArray());

            try
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch (IOException ex)
            {
                // Best-effort: don't block removing the record just because the file is locked
                // or already gone — log it so an admin can clean up storage manually if needed.
                _logger.LogWarning(ex, "Could not delete physical file for attachment {AttachmentId} at {Path}", attachmentId, physicalPath);
            }

            _context.CallAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return AttachmentDeleteResult.Success();
        }
    }
}
