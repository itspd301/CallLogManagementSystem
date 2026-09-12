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
    public class StatusController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public StatusController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var statuses = await _context.Statuses.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync();
            return View(statuses);
        }

        [HttpGet]
        public IActionResult Create() => View(new StatusFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StatusFormViewModel model)
        {
            if (await _context.Statuses.AnyAsync(s => s.Name == model.Name.Trim()))
            {
                ModelState.AddModelError(nameof(model.Name), "A status with this name already exists.");
            }

            if (await _context.Statuses.AnyAsync(s => s.Code == model.Code.Trim().ToUpper()))
            {
                ModelState.AddModelError(nameof(model.Code), "A status with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var status = new Status
            {
                Name = model.Name.Trim(),
                Code = model.Code.Trim().ToUpperInvariant(),
                ColorCode = model.ColorCode,
                SortOrder = model.SortOrder,
                IsActive = model.IsActive
            };

            _context.Statuses.Add(status);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Status", status.Id.ToString(), newValue: $"Created: {status.Name}");

            TempData["StatusMessage"] = $"Status \"{status.Name}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var status = await _context.Statuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (status == null)
            {
                return NotFound();
            }

            return View(new StatusFormViewModel
            {
                Id = status.Id,
                Name = status.Name,
                Code = status.Code,
                ColorCode = status.ColorCode,
                SortOrder = status.SortOrder,
                IsActive = status.IsActive,
                IsEdit = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StatusFormViewModel model)
        {
            model.IsEdit = true;
            if (id != model.Id)
            {
                return BadRequest();
            }

            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Id == id);
            if (status == null)
            {
                return NotFound();
            }

            if (await _context.Statuses.AnyAsync(s => s.Name == model.Name.Trim() && s.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "A status with this name already exists.");
            }

            if (await _context.Statuses.AnyAsync(s => s.Code == model.Code.Trim().ToUpper() && s.Id != id))
            {
                ModelState.AddModelError(nameof(model.Code), "A status with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var oldValue = $"{status.Name} ({status.Code})";
            status.Name = model.Name.Trim();
            status.Code = model.Code.Trim().ToUpperInvariant();
            status.ColorCode = model.ColorCode;
            status.SortOrder = model.SortOrder;
            status.IsActive = model.IsActive;
            status.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Status", status.Id.ToString(), oldValue: oldValue, newValue: $"{status.Name} ({status.Code})");

            TempData["StatusMessage"] = $"Status \"{status.Name}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Id == id);
            if (status == null)
            {
                return NotFound();
            }

            status.IsActive = !status.IsActive;
            status.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Status", status.Id.ToString(), newValue: status.IsActive ? "Activated" : "Deactivated");

            TempData["StatusMessage"] = $"Status \"{status.Name}\" {(status.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
