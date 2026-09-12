using System.Security.Claims;
using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Models.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CallLogManagementSystem.Services
{
    // Adds FullName/Designation as claims on the auth cookie at sign-in time, so every
    // page can read them straight off ClaimsPrincipal without touching the database.
    public class ApplicationUserClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(AppClaimTypes.FullName, user.FullName));
            identity.AddClaim(new Claim(AppClaimTypes.Designation, user.Designation ?? string.Empty));
            return identity;
        }
    }
}
