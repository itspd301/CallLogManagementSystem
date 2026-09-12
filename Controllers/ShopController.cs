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
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ShopController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Shops.AsNoTracking().Include(s => s.Location).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(s => EF.Functions.Like(s.Name, $"%{term}%") || EF.Functions.Like(s.Code, $"%{term}%"));
            }

            var rows = await query
                .OrderBy(s => s.Location!.Name).ThenBy(s => s.Name)
                .Select(s => new ShopRowVm
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    LocationName = s.Location!.Name,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            ViewBag.Search = search;
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ShopFormViewModel { LocationOptions = await GetLocationOptionsAsync() };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShopFormViewModel model)
        {
            if (await _context.Shops.AnyAsync(s => s.LocationId == model.LocationId && s.Code == model.Code.Trim().ToUpper()))
            {
                ModelState.AddModelError(nameof(model.Code), "A shop with this code already exists at the selected location.");
            }

            if (!ModelState.IsValid)
            {
                model.LocationOptions = await GetLocationOptionsAsync();
                return View(model);
            }

            var shop = new Shop
            {
                Name = model.Name.Trim(),
                Code = model.Code.Trim().ToUpperInvariant(),
                LocationId = model.LocationId,
                IsActive = model.IsActive
            };

            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Shop", shop.Id.ToString(), newValue: $"Created: {shop.Name}");

            TempData["StatusMessage"] = $"Shop \"{shop.Name}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var shop = await _context.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (shop == null)
            {
                return NotFound();
            }

            return View(new ShopFormViewModel
            {
                Id = shop.Id,
                Name = shop.Name,
                Code = shop.Code,
                LocationId = shop.LocationId,
                IsActive = shop.IsActive,
                IsEdit = true,
                LocationOptions = await GetLocationOptionsAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ShopFormViewModel model)
        {
            model.IsEdit = true;
            if (id != model.Id)
            {
                return BadRequest();
            }

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == id);
            if (shop == null)
            {
                return NotFound();
            }

            if (await _context.Shops.AnyAsync(s => s.LocationId == model.LocationId && s.Code == model.Code.Trim().ToUpper() && s.Id != id))
            {
                ModelState.AddModelError(nameof(model.Code), "A shop with this code already exists at the selected location.");
            }

            if (!ModelState.IsValid)
            {
                model.LocationOptions = await GetLocationOptionsAsync();
                return View(model);
            }

            var oldValue = $"{shop.Name} ({shop.Code})";
            shop.Name = model.Name.Trim();
            shop.Code = model.Code.Trim().ToUpperInvariant();
            shop.LocationId = model.LocationId;
            shop.IsActive = model.IsActive;
            shop.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Shop", shop.Id.ToString(), oldValue: oldValue, newValue: $"{shop.Name} ({shop.Code})");

            TempData["StatusMessage"] = $"Shop \"{shop.Name}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == id);
            if (shop == null)
            {
                return NotFound();
            }

            shop.IsActive = !shop.IsActive;
            shop.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(AuditAction.MasterChanged, "Shop", shop.Id.ToString(), newValue: shop.IsActive ? "Activated" : "Deactivated");

            TempData["StatusMessage"] = $"Shop \"{shop.Name}\" {(shop.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetLocationOptionsAsync()
        {
            return await _context.Locations.AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();
        }
    }
}
