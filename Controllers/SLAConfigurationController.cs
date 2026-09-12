using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Masters;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    // One SLA configuration per Priority (Section 28) — this is an upsert-on-Edit master,
    // not a free-form Add/list, since an SLA row without a Priority has no meaning and every
    // Priority should have exactly one.
    [Authorize(Policy = PolicyNames.AccessMasterConfiguration)]
    public class SLAConfigurationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public SLAConfigurationController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var priorities = await _context.Priorities.AsNoTracking()
                .OrderByDescending(p => p.Level)
                .ToListAsync();

            var configs = await _context.SLAConfigurations.AsNoTracking().ToListAsync();

            var rows = priorities.Select(p =>
            {
                var config = configs.FirstOrDefault(c => c.PriorityId == p.Id);
                return new SLAConfigurationRowVm
                {
                    PriorityId = p.Id,
                    PriorityName = p.Name,
                    PriorityColor = p.ColorCode,
                    HasConfiguration = config != null,
                    ResponseMinutes = config?.ResponseMinutes,
                    ResolutionMinutes = config?.ResolutionMinutes,
                    IsActive = config?.IsActive ?? false
                };
            }).ToList();

            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int priorityId)
        {
            var priority = await _context.Priorities.AsNoTracking().FirstOrDefaultAsync(p => p.Id == priorityId);
            if (priority == null)
            {
                return NotFound();
            }

            var config = await _context.SLAConfigurations.AsNoTracking().FirstOrDefaultAsync(c => c.PriorityId == priorityId);

            return View(new SLAConfigurationFormViewModel
            {
                PriorityId = priority.Id,
                PriorityName = priority.Name,
                ResponseMinutes = config?.ResponseMinutes ?? 0,
                ResolutionMinutes = config?.ResolutionMinutes ?? 0,
                IsActive = config?.IsActive ?? true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int priorityId, SLAConfigurationFormViewModel model)
        {
            if (priorityId != model.PriorityId)
            {
                return BadRequest();
            }

            var priority = await _context.Priorities.AsNoTracking().FirstOrDefaultAsync(p => p.Id == priorityId);
            if (priority == null)
            {
                return NotFound();
            }
            model.PriorityName = priority.Name;

            if (model.ResolutionMinutes < model.ResponseMinutes)
            {
                ModelState.AddModelError(nameof(model.ResolutionMinutes), "Resolution time must be greater than or equal to response time.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var config = await _context.SLAConfigurations.FirstOrDefaultAsync(c => c.PriorityId == priorityId);
            var isNew = config == null;

            if (config == null)
            {
                config = new SLAConfiguration { PriorityId = priorityId };
                _context.SLAConfigurations.Add(config);
            }

            config.ResponseMinutes = model.ResponseMinutes;
            config.ResolutionMinutes = model.ResolutionMinutes;
            config.IsActive = model.IsActive;
            config.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(
                AuditAction.MasterChanged,
                "SLAConfiguration",
                config.Id.ToString(),
                newValue: $"{priority.Name}: Response {model.ResponseMinutes}m, Resolution {model.ResolutionMinutes}m");

            TempData["StatusMessage"] = $"SLA for \"{priority.Name}\" {(isNew ? "created" : "updated")} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
