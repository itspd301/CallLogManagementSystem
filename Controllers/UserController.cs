using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    // Section 25/31 — Admin only. This is the one place a new system account gets created;
    // Employees (Section 9's "Problem Reported By") and Engineers (support-staff profiles)
    // are separate masters (Phase 10) and unaffected by anything here.
    [Authorize(Policy = PolicyNames.ManageUsers)]
    public class UserController : Controller
    {
        private static readonly string[] AllRoles =
        {
            RoleNames.Admin, RoleNames.SupportManager, RoleNames.SupportEngineer, RoleNames.Supervisor, RoleNames.Viewer
        };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService, ICurrentUserService currentUser)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? role, bool? isActive, int page = 1, int pageSize = 25)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u => EF.Functions.Like(u.FullName, $"%{term}%") || EF.Functions.Like(u.EmployeeId, $"%{term}%"));
            }

            if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive);

            page = Math.Max(1, page);
            pageSize = pageSize is > 0 and <= 200 ? pageSize : 25;

            // Role lives in AspNetUserRoles, not on ApplicationUser directly — filter/join in
            // memory over this (small, admin-only) table rather than a raw SQL join.
            var allUsers = await query.OrderBy(u => u.FullName).ToListAsync();
            var locationNames = await _context.Locations.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Name);

            var rows = new List<UserRowVm>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault();

                if (!string.IsNullOrEmpty(role) && primaryRole != role)
                {
                    continue;
                }

                rows.Add(new UserRowVm
                {
                    Id = user.Id,
                    EmployeeId = user.EmployeeId,
                    FullName = user.FullName,
                    Designation = user.Designation,
                    LocationName = user.LocationId.HasValue && locationNames.TryGetValue(user.LocationId.Value, out var name) ? name : null,
                    Role = primaryRole,
                    IsActive = user.IsActive,
                    LastLoginDate = user.LastLoginDate
                });
            }

            var totalCount = rows.Count;
            var paged = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new UserListViewModel
            {
                Items = paged,
                Search = search,
                Role = role,
                IsActive = isActive,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                RoleOptions = AllRoles.Select(r => new SelectListItem { Value = r, Text = r }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildFormAsync(new UserFormViewModel { IsActive = true }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "An initial password is required for a new user.");
            }

            if (await _context.Users.AnyAsync(u => u.EmployeeId == model.EmployeeId.Trim()))
            {
                ModelState.AddModelError(nameof(model.EmployeeId), "A user with this Employee ID already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model));
            }

            var user = new ApplicationUser
            {
                UserName = model.EmployeeId.Trim(),
                EmployeeId = model.EmployeeId.Trim(),
                FullName = model.FullName.Trim(),
                Designation = model.Designation?.Trim() ?? string.Empty,
                LocationId = model.LocationId,
                IsActive = model.IsActive,
                EmailConfirmed = true,
                CreatedDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(await BuildFormAsync(model));
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(AuditAction.UserChanged, nameof(ApplicationUser), user.Id,
                newValue: $"Created: {user.FullName} ({user.EmployeeId}), Role: {model.Role}", userId: _currentUser.UserId);

            TempData["StatusMessage"] = $"User \"{user.FullName}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserFormViewModel
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                FullName = user.FullName,
                Designation = user.Designation,
                LocationId = user.LocationId,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive,
                IsEdit = true
            };

            return View(await BuildFormAsync(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserFormViewModel model)
        {
            model.IsEdit = true;
            if (id != model.Id)
            {
                return BadRequest();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            // Deactivating yourself would risk locking out the only admin who can undo it.
            if (id == _currentUser.UserId && !model.IsActive)
            {
                ModelState.AddModelError(nameof(model.IsActive), "You cannot deactivate your own account.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildFormAsync(model));
            }

            var oldRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            user.FullName = model.FullName.Trim();
            user.Designation = model.Designation?.Trim() ?? string.Empty;
            user.LocationId = model.LocationId;
            user.IsActive = model.IsActive;

            await _userManager.UpdateAsync(user);

            if (oldRole != model.Role)
            {
                if (!string.IsNullOrEmpty(oldRole))
                {
                    await _userManager.RemoveFromRoleAsync(user, oldRole);
                }
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            await _auditService.LogAsync(AuditAction.UserChanged, nameof(ApplicationUser), user.Id,
                oldValue: oldRole, newValue: model.Role, userId: _currentUser.UserId);

            TempData["StatusMessage"] = $"User \"{user.FullName}\" updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            if (id == _currentUser.UserId)
            {
                TempData["StatusMessage"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(AuditAction.UserChanged, nameof(ApplicationUser), user.Id,
                newValue: user.IsActive ? "Activated" : "Deactivated", userId: _currentUser.UserId);

            TempData["StatusMessage"] = $"User \"{user.FullName}\" {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(new ResetPasswordAdminViewModel { UserId = user.Id, EmployeeId = user.EmployeeId, FullName = user.FullName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, ResetPasswordAdminViewModel model)
        {
            if (id != model.UserId)
            {
                return BadRequest();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.EmployeeId = user.EmployeeId;
                model.FullName = user.FullName;
                return View(model);
            }

            // Admin-forced reset: remove the existing hash and set a new one directly, rather
            // than the token-based self-service flow (there's no email service to deliver a
            // token — see Phase 7's ForgotPassword page, which sends users here).
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                model.EmployeeId = user.EmployeeId;
                model.FullName = user.FullName;
                return View(model);
            }

            await _auditService.LogAsync(AuditAction.UserChanged, nameof(ApplicationUser), user.Id,
                newValue: "Password reset by administrator", userId: _currentUser.UserId);

            TempData["StatusMessage"] = $"Password reset for \"{user.FullName}\".";
            return RedirectToAction(nameof(Index));
        }

        private async Task<UserFormViewModel> BuildFormAsync(UserFormViewModel model)
        {
            model.LocationOptions = await _context.Locations.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync();

            model.RoleOptions = AllRoles.Select(r => new SelectListItem { Value = r, Text = r }).ToList();

            return model;
        }
    }
}
