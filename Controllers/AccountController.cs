using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.EmployeeId.Trim());

            if (user == null)
            {
                _logger.LogWarning("Login failed: unknown Employee ID {EmployeeId}", model.EmployeeId);
                await _auditService.LogAsync(AuditAction.LoginFailed, nameof(ApplicationUser), model.EmployeeId, newValue: "Unknown Employee ID");
                ModelState.AddModelError(string.Empty, "Invalid Employee ID or Password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login blocked: deactivated account {EmployeeId}", model.EmployeeId);
                await _auditService.LogAsync(AuditAction.LoginFailed, nameof(ApplicationUser), user.Id, newValue: "Account deactivated", userId: user.Id);
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginDate = DateTime.Now;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {EmployeeId} logged in", user.EmployeeId);
                await _auditService.LogAsync(AuditAction.Login, nameof(ApplicationUser), user.Id, userId: user.Id);

                return RedirectToLocal(model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login blocked: {EmployeeId} is locked out", model.EmployeeId);
                await _auditService.LogAsync(AuditAction.LoginFailed, nameof(ApplicationUser), user.Id, newValue: "Locked out", userId: user.Id);
                ModelState.AddModelError(string.Empty, "This account is locked due to multiple failed login attempts. Please try again in 15 minutes.");
                return View(model);
            }

            _logger.LogWarning("Login failed: bad password for {EmployeeId}", model.EmployeeId);
            await _auditService.LogAsync(AuditAction.LoginFailed, nameof(ApplicationUser), user.Id, newValue: "Invalid password", userId: user.Id);
            ModelState.AddModelError(string.Empty, "Invalid Employee ID or Password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = _userManager.GetUserId(User);
            var employeeId = User.Identity?.Name;

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {EmployeeId} logged out", employeeId);
            await _auditService.LogAsync(AuditAction.Logout, nameof(ApplicationUser), userId, userId: userId);

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var location = user.LocationId.HasValue
                ? await _context.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == user.LocationId)
                : null;

            var model = new ProfileViewModel
            {
                EmployeeId = user.EmployeeId,
                FullName = user.FullName,
                Designation = user.Designation,
                LocationName = location?.Name,
                Roles = await _userManager.GetRolesAsync(user),
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            await _auditService.LogAsync(AuditAction.UserChanged, nameof(ApplicationUser), user.Id, newValue: "Password changed", userId: user.Id);
            _logger.LogInformation("User {EmployeeId} changed their password", user.EmployeeId);

            TempData["StatusMessage"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(ChangePassword));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
