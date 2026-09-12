using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Masters;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    [Authorize(Policy = PolicyNames.AccessMasterConfiguration)]
    public class EngineerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public EngineerController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var rows = await _context.Engineers.AsNoTracking()
                .Include(e => e.ApplicationUser)
                .Include(e => e.Location)
                .OrderBy(e => e.ApplicationUser!.FullName)
                .Select(e => new EngineerRowVm
                {
                    Id = e.Id,
                    FullName = e.ApplicationUser!.FullName,
                    EmployeeId = e.ApplicationUser.EmployeeId,
                    Specialization = e.Specialization,
                    LocationName = e.Location != null ? e.Location.Name : null,
                    IsActive = e.IsActive
                })
                .ToListAsync();

            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildFormAsync(new EngineerFormViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EngineerFormViewModel model)
        {
            if (await _context.Engineers.AnyAsync(e => e.ApplicationUserId == model.ApplicationUserId))
            {
                ModelState.AddModelError(nameof(model.ApplicationUserId), "This user already has an Engineer profile.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model));
            }

            var engineer = new Engineer
            {
                ApplicationUserId = model.ApplicationUserId,
                Specialization = model.Specialization?.Trim(),
                LocationId = model.LocationId,
                IsActive = model.IsActive
            };

            _context.Engineers.Add(engineer);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Engineer", engineer.Id.ToString(), newValue: "Created");

            TempData["StatusMessage"] = "Engineer profile created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var engineer = await _context.Engineers.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (engineer == null)
            {
                return NotFound();
            }

            var model = new EngineerFormViewModel
            {
                Id = engineer.Id,
                ApplicationUserId = engineer.ApplicationUserId,
                Specialization = engineer.Specialization,
                LocationId = engineer.LocationId,
                IsActive = engineer.IsActive,
                IsEdit = true
            };

            return View(await BuildFormAsync(model, engineer.ApplicationUserId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EngineerFormViewModel model)
        {
            model.IsEdit = true;
            if (id != model.Id)
            {
                return BadRequest();
            }

            var engineer = await _context.Engineers.FirstOrDefaultAsync(e => e.Id == id);
            if (engineer == null)
            {
                return NotFound();
            }

            if (await _context.Engineers.AnyAsync(e => e.ApplicationUserId == model.ApplicationUserId && e.Id != id))
            {
                ModelState.AddModelError(nameof(model.ApplicationUserId), "This user already has an Engineer profile.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model, engineer.ApplicationUserId));
            }

            engineer.ApplicationUserId = model.ApplicationUserId;
            engineer.Specialization = model.Specialization?.Trim();
            engineer.LocationId = model.LocationId;
            engineer.IsActive = model.IsActive;
            engineer.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Engineer", engineer.Id.ToString(), newValue: "Updated");

            TempData["StatusMessage"] = "Engineer profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var engineer = await _context.Engineers.FirstOrDefaultAsync(e => e.Id == id);
            if (engineer == null)
            {
                return NotFound();
            }

            engineer.IsActive = !engineer.IsActive;
            engineer.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Engineer", engineer.Id.ToString(), newValue: engineer.IsActive ? "Activated" : "Deactivated");

            TempData["StatusMessage"] = $"Engineer profile {(engineer.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<EngineerFormViewModel> BuildFormAsync(EngineerFormViewModel model, string? keepUserId = null)
        {
            var linkedUserIds = await _context.Engineers.AsNoTracking()
                .Where(e => keepUserId == null || e.ApplicationUserId != keepUserId)
                .Select(e => e.ApplicationUserId)
                .ToListAsync();

            model.UserOptions = await _context.Users.AsNoTracking()
                .Where(u => u.IsActive && !linkedUserIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .Select(u => new SelectListItem { Value = u.Id, Text = u.FullName + " (" + u.EmployeeId + ")" })
                .ToListAsync();

            model.LocationOptions = await _context.Locations.AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();

            return model;
        }
    }
}
