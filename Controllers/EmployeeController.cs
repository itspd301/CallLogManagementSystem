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
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public EmployeeController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Employees.AsNoTracking()
                .Include(e => e.Location)
                .Include(e => e.Shop)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(e => EF.Functions.Like(e.FullName, $"%{term}%") || EF.Functions.Like(e.EmployeeCode, $"%{term}%"));
            }

            var rows = await query
                .OrderBy(e => e.FullName)
                .Select(e => new EmployeeRowVm
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Designation = e.Designation,
                    LocationName = e.Location!.Name,
                    ShopName = e.Shop != null ? e.Shop.Name : null,
                    IsActive = e.IsActive
                })
                .ToListAsync();

            ViewBag.Search = search;
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildFormAsync(new EmployeeFormViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeFormViewModel model)
        {
            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode.Trim()))
            {
                ModelState.AddModelError(nameof(model.EmployeeCode), "An employee with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model));
            }

            var employee = new Employee
            {
                EmployeeCode = model.EmployeeCode.Trim(),
                FullName = model.FullName.Trim(),
                Designation = model.Designation?.Trim(),
                LocationId = model.LocationId,
                ShopId = model.ShopId,
                ContactNumber = model.ContactNumber?.Trim(),
                IsActive = model.IsActive
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Employee", employee.Id.ToString(), newValue: $"Created: {employee.FullName}");

            TempData["StatusMessage"] = $"Employee \"{employee.FullName}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            var model = new EmployeeFormViewModel
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                Designation = employee.Designation,
                LocationId = employee.LocationId,
                ShopId = employee.ShopId,
                ContactNumber = employee.ContactNumber,
                IsActive = employee.IsActive,
                IsEdit = true
            };

            return View(await BuildFormAsync(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
        {
            model.IsEdit = true;
            if (id != model.Id)
            {
                return BadRequest();
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            if (await _context.Employees.AnyAsync(e => e.EmployeeCode == model.EmployeeCode.Trim() && e.Id != id))
            {
                ModelState.AddModelError(nameof(model.EmployeeCode), "An employee with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model));
            }

            var oldValue = employee.FullName;
            employee.EmployeeCode = model.EmployeeCode.Trim();
            employee.FullName = model.FullName.Trim();
            employee.Designation = model.Designation?.Trim();
            employee.LocationId = model.LocationId;
            employee.ShopId = model.ShopId;
            employee.ContactNumber = model.ContactNumber?.Trim();
            employee.IsActive = model.IsActive;
            employee.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Employee", employee.Id.ToString(), oldValue: oldValue, newValue: employee.FullName);

            TempData["StatusMessage"] = $"Employee \"{employee.FullName}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = !employee.IsActive;
            employee.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Employee", employee.Id.ToString(), newValue: employee.IsActive ? "Activated" : "Deactivated");

            TempData["StatusMessage"] = $"Employee \"{employee.FullName}\" {(employee.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<EmployeeFormViewModel> BuildFormAsync(EmployeeFormViewModel model)
        {
            model.LocationOptions = await _context.Locations.AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();

            model.ShopOptions = await _context.Shops.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();

            return model;
        }
    }
}
