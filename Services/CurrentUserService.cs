using System.Security.Claims;
using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Interfaces;

namespace CallLogManagementSystem.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? EmployeeId => User?.Identity?.Name;
        public string? FullName => User?.FindFirstValue(AppClaimTypes.FullName);
        public string? Designation => User?.FindFirstValue(AppClaimTypes.Designation);
        public string? IPAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

        private static readonly string[] RolePriority =
        {
            RoleNames.Admin,
            RoleNames.SupportManager,
            RoleNames.SupportEngineer,
            RoleNames.Supervisor,
            RoleNames.Viewer
        };

        public string? PrimaryRole => RolePriority.FirstOrDefault(IsInRole);
    }
}
