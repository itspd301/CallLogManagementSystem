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
    public class ProblemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ProblemController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Problems.AsNoTracking()
                .Include(p => p.ProblemCategory)
                .Include(p => p.Module)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{term}%"));
            }

            var rows = await query
                .OrderBy(p => p.Name)
                .Select(p => new ProblemRowVm
                {
                    Id = p.Id,
                    Name = p.Name,
                    ProblemCategoryName = p.ProblemCategory!.Name,
                    ModuleName = p.Module != null ? p.Module.Name : null,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            ViewBag.Search = search;
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildFormAsync(new ProblemFormViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProblemFormViewModel model)
        {
            if (await _context.Problems.AnyAsync(p => p.Name == model.Name.Trim() && p.ProblemCategoryId == model.ProblemCategoryId))
            {
                ModelState.AddModelError(nameof(model.Name), "A problem with this name already exists in this category.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model));
            }

            var problem = new Problem
            {
                Name = model.Name.Trim(),
                ProblemCategoryId = model.ProblemCategoryId,
                ModuleId = model.ModuleId,
                IsActive = model.IsActive
            };

            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Problem", problem.Id.ToString(), newValue: $"Created: {problem.Name}");

            TempData["StatusMessage"] = $"Problem \"{problem.Name}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var problem = await _context.Problems.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (problem == null)
            {
                return NotFound();
            }

            var model = new ProblemFormViewModel
            {
                Id = problem.Id,
                Name = problem.Name,
                ProblemCategoryId = problem.ProblemCategoryId,
                ModuleId = problem.ModuleId,
                IsActive = problem.IsActive,
                IsEdit = true
            };

            return View(await BuildFormAsync(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProblemFormViewModel model)
        {
            model.IsEdit = true;
            if (id != model.Id)
            {
                return BadRequest();
            }

            var problem = await _context.Problems.FirstOrDefaultAsync(p => p.Id == id);
            if (problem == null)
            {
                return NotFound();
            }

            if (await _context.Problems.AnyAsync(p => p.Name == model.Name.Trim() && p.ProblemCategoryId == model.ProblemCategoryId && p.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "A problem with this name already exists in this category.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model));
            }

            var oldValue = problem.Name;
            problem.Name = model.Name.Trim();
            problem.ProblemCategoryId = model.ProblemCategoryId;
            problem.ModuleId = model.ModuleId;
            problem.IsActive = model.IsActive;
            problem.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Problem", problem.Id.ToString(), oldValue: oldValue, newValue: problem.Name);

            TempData["StatusMessage"] = $"Problem \"{problem.Name}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var problem = await _context.Problems.FirstOrDefaultAsync(p => p.Id == id);
            if (problem == null)
            {
                return NotFound();
            }

            problem.IsActive = !problem.IsActive;
            problem.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Problem", problem.Id.ToString(), newValue: problem.IsActive ? "Activated" : "Deactivated");

            TempData["StatusMessage"] = $"Problem \"{problem.Name}\" {(problem.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<ProblemFormViewModel> BuildFormAsync(ProblemFormViewModel model)
        {
            model.ProblemCategoryOptions = await _context.ProblemCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

            model.ModuleOptions = await _context.Modules.AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name })
                .ToListAsync();

            return model;
        }
    }
}
