using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers.Masters
{
    // Shared Index/Create/Edit/ToggleActive for every Master shaped as Name(+Code)+IsActive.
    // Concrete controllers (LocationController, ModuleController, ApplicationTypeController,
    // CallCategoryController, ProblemCategoryController) only need to name the entity — this
    // base handles search, sort, pagination, uniqueness validation, and audit logging once.
    [Authorize(Policy = PolicyNames.AccessMasterConfiguration)]
    public abstract class SimpleMasterControllerBase<TEntity> : Controller
        where TEntity : MasterEntity, INamedMasterEntity, new()
    {
        protected readonly ApplicationDbContext Context;
        protected readonly IAuditService AuditService;

        protected SimpleMasterControllerBase(ApplicationDbContext context, IAuditService auditService)
        {
            Context = context;
            AuditService = auditService;
        }

        protected abstract string EntityDisplayName { get; }
        protected virtual bool HasCode => typeof(ICodedMasterEntity).IsAssignableFrom(typeof(TEntity));
        private string ControllerName => GetType().Name.Replace("Controller", string.Empty);

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string sort = "Name", string dir = "asc", int page = 1, int pageSize = 20)
        {
            var query = Context.Set<TEntity>().AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                // Name/Code are interface-only members (INamedMasterEntity/ICodedMasterEntity) —
                // e.Name would bind to the interface property, which EF Core's SQL translator
                // cannot reliably map back to the concrete entity's column. EF.Property(e, "Name")
                // looks the column up by name against the actual mapped entity type instead.
                // The two Where() calls are chosen by the C# ternary before either lambda is
                // ever handed to EF's provider, so the Code branch never runs for entities with
                // no Code column at all.
                query = HasCode
                    ? query.Where(e => EF.Functions.Like(EF.Property<string>(e, "Name"), $"%{term}%") || EF.Functions.Like(EF.Property<string>(e, "Code"), $"%{term}%"))
                    : query.Where(e => EF.Functions.Like(EF.Property<string>(e, "Name"), $"%{term}%"));
            }

            query = (sort, dir) switch
            {
                ("Name", "desc") => query.OrderByDescending(e => EF.Property<string>(e, "Name")),
                ("IsActive", "asc") => query.OrderBy(e => e.IsActive).ThenBy(e => EF.Property<string>(e, "Name")),
                ("IsActive", "desc") => query.OrderByDescending(e => e.IsActive).ThenBy(e => EF.Property<string>(e, "Name")),
                _ => query.OrderBy(e => EF.Property<string>(e, "Name"))
            };

            var totalCount = await query.CountAsync();
            page = Math.Max(1, page);
            pageSize = pageSize is > 0 and <= 200 ? pageSize : 20;

            // Materialize the (small) page of real TEntity objects first, then map to the row
            // VM in plain C# — the ICodedMasterEntity cast only ever runs against real in-memory
            // objects here, never inside an expression tree EF has to translate to SQL.
            var pageEntities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = pageEntities
                .Select(e => new SimpleMasterRowVm
                {
                    Id = e.Id,
                    Name = e.Name,
                    Code = e is ICodedMasterEntity coded ? coded.Code : null,
                    IsActive = e.IsActive
                })
                .ToList();

            var vm = new SimpleMasterListViewModel
            {
                EntityDisplayName = EntityDisplayName,
                ControllerName = ControllerName,
                HasCode = HasCode,
                Items = items,
                Search = search,
                Sort = sort,
                Dir = dir,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View("~/Views/Shared/Masters/SimpleMasterIndex.cshtml", vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new SimpleMasterFormViewModel
            {
                HasCode = HasCode,
                EntityDisplayName = EntityDisplayName,
                ControllerName = ControllerName,
                IsActive = true,
                IsEdit = false
            };
            return View("~/Views/Shared/Masters/SimpleMasterForm.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SimpleMasterFormViewModel model)
        {
            model.HasCode = HasCode;
            model.EntityDisplayName = EntityDisplayName;
            model.ControllerName = ControllerName;

            if (HasCode && string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "Code is required.");
            }

            if (await NameExistsAsync(model.Name, null))
            {
                ModelState.AddModelError(nameof(model.Name), $"A {EntityDisplayName.ToLowerInvariant()} with this name already exists.");
            }

            if (HasCode && !string.IsNullOrWhiteSpace(model.Code) && await CodeExistsAsync(model.Code, null))
            {
                ModelState.AddModelError(nameof(model.Code), $"A {EntityDisplayName.ToLowerInvariant()} with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Shared/Masters/SimpleMasterForm.cshtml", model);
            }

            var entity = new TEntity
            {
                Name = model.Name.Trim(),
                IsActive = model.IsActive
            };

            if (HasCode)
            {
                ((ICodedMasterEntity)entity).Code = model.Code!.Trim().ToUpperInvariant();
            }

            Context.Set<TEntity>().Add(entity);
            await Context.SaveChangesAsync();

            await AuditService.LogAsync(AuditAction.MasterChanged, EntityDisplayName, entity.Id.ToString(), newValue: $"Created: {entity.Name}");

            TempData["StatusMessage"] = $"{EntityDisplayName} \"{entity.Name}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await Context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
            {
                return NotFound();
            }

            var vm = new SimpleMasterFormViewModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = HasCode ? ((ICodedMasterEntity)entity).Code : null,
                IsActive = entity.IsActive,
                HasCode = HasCode,
                EntityDisplayName = EntityDisplayName,
                ControllerName = ControllerName,
                IsEdit = true
            };
            return View("~/Views/Shared/Masters/SimpleMasterForm.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SimpleMasterFormViewModel model)
        {
            model.HasCode = HasCode;
            model.EntityDisplayName = EntityDisplayName;
            model.ControllerName = ControllerName;
            model.IsEdit = true;

            if (id != model.Id)
            {
                return BadRequest();
            }

            var entity = await Context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
            {
                return NotFound();
            }

            if (HasCode && string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "Code is required.");
            }

            if (await NameExistsAsync(model.Name, id))
            {
                ModelState.AddModelError(nameof(model.Name), $"A {EntityDisplayName.ToLowerInvariant()} with this name already exists.");
            }

            if (HasCode && !string.IsNullOrWhiteSpace(model.Code) && await CodeExistsAsync(model.Code, id))
            {
                ModelState.AddModelError(nameof(model.Code), $"A {EntityDisplayName.ToLowerInvariant()} with this code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Shared/Masters/SimpleMasterForm.cshtml", model);
            }

            var oldName = entity.Name;
            var oldCode = HasCode ? ((ICodedMasterEntity)entity).Code : null;

            entity.Name = model.Name.Trim();
            entity.IsActive = model.IsActive;
            entity.ModifiedDate = DateTime.Now;

            if (HasCode)
            {
                ((ICodedMasterEntity)entity).Code = model.Code!.Trim().ToUpperInvariant();
            }

            await Context.SaveChangesAsync();

            await AuditService.LogAsync(
                AuditAction.MasterChanged,
                EntityDisplayName,
                entity.Id.ToString(),
                oldValue: HasCode ? $"{oldName} ({oldCode})" : oldName,
                newValue: HasCode ? $"{entity.Name} ({((ICodedMasterEntity)entity).Code})" : entity.Name);

            TempData["StatusMessage"] = $"{EntityDisplayName} \"{entity.Name}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var entity = await Context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
            {
                return NotFound();
            }

            entity.IsActive = !entity.IsActive;
            entity.ModifiedDate = DateTime.Now;
            await Context.SaveChangesAsync();

            await AuditService.LogAsync(
                AuditAction.MasterChanged,
                EntityDisplayName,
                entity.Id.ToString(),
                newValue: entity.IsActive ? "Activated" : "Deactivated");

            TempData["StatusMessage"] = $"{EntityDisplayName} \"{entity.Name}\" {(entity.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> NameExistsAsync(string name, int? excludeId)
        {
            var trimmed = name.Trim();
            return await Context.Set<TEntity>()
                .AnyAsync(e => EF.Property<string>(e, "Name") == trimmed && (excludeId == null || e.Id != excludeId));
        }

        private async Task<bool> CodeExistsAsync(string code, int? excludeId)
        {
            // Master tables are small (a handful of rows) — fetching them into memory for a
            // uniqueness check is cheap and sidesteps asking EF to translate an interface cast.
            var trimmed = code.Trim().ToUpperInvariant();
            var all = await Context.Set<TEntity>().AsNoTracking().ToListAsync();
            return all.OfType<ICodedMasterEntity>()
                .Any(e => e.Code == trimmed && (excludeId == null || e.Id != excludeId));
        }
    }
}
