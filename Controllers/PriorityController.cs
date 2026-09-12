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
    [Authorize(Policy = PolicyNames.AccessMasterConfiguration)]
    public class PriorityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public PriorityController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var priorities = await _context.Priorities.AsNoTracking().OrderByDescending(p => p.Level).ToListAsync();
            return View(priorities);
        }

        [HttpGet]
        public IActionResult Create() => View(new PriorityFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PriorityFormViewModel model)
        {
            if (await _context.Priorities.AnyAsync(p => p.Name == model.Name.Trim()))
            {
                ModelState.AddModelError(nameof(model.Name), "A priority with this name already exists.");
            }

            if (await _context.Priorities.AnyAsync(p => p.Code == model.Code.Trim().ToUpper()))
            {
                ModelState.AddModelError(nameof(model.Code), "A priority with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var priority = new Priority
            {
                Name = model.Name.Trim(),
                Code = model.Code.Trim().ToUpperInvariant(),
                Level = model.Level,
                ColorCode = model.ColorCode,
                IsActive = model.IsActive
            };

            _context.Priorities.Add(priority);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Priority", priority.Id.ToString(), newValue: $"Created: {priority.Name}");

            TempData["StatusMessage"] = $"Priority \"{priority.Name}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var priority = await _context.Priorities.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (priority == null)
            {
                return NotFound();
            }

            return View(new PriorityFormViewModel
            {
                Id = priority.Id,
                Name = priority.Name,
                Code = priority.Code,
                Level = priority.Level,
                ColorCode = priority.ColorCode,
                IsActive = priority.IsActive,
                IsEdit = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PriorityFormViewModel model)
        {
            model.IsEdit = true;
            if (id != model.Id)
            {
                return BadRequest();
            }

            var priority = await _context.Priorities.FirstOrDefaultAsync(p => p.Id == id);
            if (priority == null)
            {
                return NotFound();
            }

            if (await _context.Priorities.AnyAsync(p => p.Name == model.Name.Trim() && p.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "A priority with this name already exists.");
            }

            if (await _context.Priorities.AnyAsync(p => p.Code == model.Code.Trim().ToUpper() && p.Id != id))
            {
                ModelState.AddModelError(nameof(model.Code), "A priority with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var oldValue = $"{priority.Name} ({priority.Code})";
            priority.Name = model.Name.Trim();
            priority.Code = model.Code.Trim().ToUpperInvariant();
            priority.Level = model.Level;
            priority.ColorCode = model.ColorCode;
            priority.IsActive = model.IsActive;
            priority.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Priority", priority.Id.ToString(), oldValue: oldValue, newValue: $"{priority.Name} ({priority.Code})");

            TempData["StatusMessage"] = $"Priority \"{priority.Name}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var priority = await _context.Priorities.FirstOrDefaultAsync(p => p.Id == id);
            if (priority == null)
            {
                return NotFound();
            }

            priority.IsActive = !priority.IsActive;
            priority.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Priority", priority.Id.ToString(), newValue: priority.IsActive ? "Activated" : "Deactivated");

            TempData["StatusMessage"] = $"Priority \"{priority.Name}\" {(priority.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
