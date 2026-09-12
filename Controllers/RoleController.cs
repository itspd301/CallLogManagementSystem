using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.ViewModels.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    // Section 25/31 — deliberately read-only. The 5 roles are hardcoded into every
    // authorization policy in Program.cs (Phase 8); letting an admin create a new role here
    // would produce a role with zero permissions (silently broken), and deleting one of the 5
    // would break every user still holding it. What IS genuinely useful — seeing who holds
    // which role, and a reference copy of the Section 7 permission matrix — is what this page
    // shows instead of pretending role CRUD is safe to offer.
    [Authorize(Policy = PolicyNames.ManageRoles)]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public RoleController(RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();

            var rows = new List<RoleRowVm>();
            foreach (var role in roles)
            {
                var count = await (from ur in _context.UserRoles
                                    where ur.RoleId == role.Id
                                    select ur).CountAsync();

                rows.Add(new RoleRowVm { Name = role.Name ?? "-", UserCount = count });
            }

            return View(new RoleListViewModel { Roles = rows });
        }
    }
}
