using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Models.Entities.Masters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Data
{
    // Phase 6 — idempotent startup seed: fixed roles, a handful of demo users covering every
    // role, and enough master data for Create Call to be exercised end-to-end immediately.
    public static class DbSeeder
    {
        private const string SeedPassword = "Pass@123"; // dev-only; changed via Phase 7 "force change on first login".

        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await SeedRolesAsync(roleManager);

            var locations = await SeedLocationsAsync(context);
            var shops = await SeedShopsAsync(context, locations);
            await SeedModulesAsync(context);
            await SeedApplicationTypesAsync(context);
            await SeedCallCategoriesAsync(context);
            var problemCategories = await SeedProblemCategoriesAsync(context);
            await SeedProblemsAsync(context, problemCategories);
            var priorities = await SeedPrioritiesAsync(context);
            await SeedStatusesAsync(context);
            await SeedSLAConfigurationsAsync(context, priorities);
            await SeedEmployeesAsync(context, locations, shops);

            await SeedUsersAndEngineersAsync(context, userManager, locations);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                RoleNames.Admin,
                RoleNames.SupportManager,
                RoleNames.SupportEngineer,
                RoleNames.Supervisor,
                RoleNames.Viewer
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task<List<Location>> SeedLocationsAsync(ApplicationDbContext context)
        {
            if (!await context.Locations.AnyAsync())
            {
                context.Locations.AddRange(
                    new Location { Name = "Nashik Plant 1", Code = "NSK1" },
                    new Location { Name = "Nashik Plant 2", Code = "NSK2" },
                    new Location { Name = "Chakan Plant", Code = "CHK1" }
                );
                await context.SaveChangesAsync();
            }

            return await context.Locations.ToListAsync();
        }

        private static async Task<List<Shop>> SeedShopsAsync(ApplicationDbContext context, List<Location> locations)
        {
            if (!await context.Shops.AnyAsync())
            {
                string[,] shopDefs =
                {
                    { "Assembly", "ASSY" },
                    { "Paint Shop", "PAINT" },
                    { "Welding", "WELD" },
                    { "Press Shop", "PRESS" },
                    { "Quality", "QLTY" },
                    { "Maintenance", "MAINT" }
                };

                var shops = new List<Shop>();
                foreach (var location in locations)
                {
                    for (var i = 0; i < shopDefs.GetLength(0); i++)
                    {
                        shops.Add(new Shop
                        {
                            Name = shopDefs[i, 0],
                            Code = shopDefs[i, 1],
                            LocationId = location.Id
                        });
                    }
                }

                context.Shops.AddRange(shops);
                await context.SaveChangesAsync();
            }

            return await context.Shops.ToListAsync();
        }

        private static async Task SeedModulesAsync(ApplicationDbContext context)
        {
            if (!await context.Modules.AnyAsync())
            {
                context.Modules.AddRange(
                    new Module { Name = "MES", Code = "MES" },
                    new Module { Name = "ERP", Code = "ERP" },
                    new Module { Name = "WMS", Code = "WMS" },
                    new Module { Name = "Barcode / Scanning System", Code = "BARCODE" },
                    new Module { Name = "Network Infrastructure", Code = "NETWORK" }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedApplicationTypesAsync(ApplicationDbContext context)
        {
            if (!await context.ApplicationTypes.AnyAsync())
            {
                context.ApplicationTypes.AddRange(
                    new ApplicationType { Name = "Web Application" },
                    new ApplicationType { Name = "Desktop Application" },
                    new ApplicationType { Name = "Mobile Application" },
                    new ApplicationType { Name = "Hardware / Device" }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedCallCategoriesAsync(ApplicationDbContext context)
        {
            if (!await context.CallCategories.AnyAsync())
            {
                context.CallCategories.AddRange(
                    new CallCategory { Name = "Incident" },
                    new CallCategory { Name = "Service Request" },
                    new CallCategory { Name = "Change Request" },
                    new CallCategory { Name = "Problem" }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task<List<ProblemCategory>> SeedProblemCategoriesAsync(ApplicationDbContext context)
        {
            if (!await context.ProblemCategories.AnyAsync())
            {
                context.ProblemCategories.AddRange(
                    new ProblemCategory { Name = "Hardware" },
                    new ProblemCategory { Name = "Software / Application" },
                    new ProblemCategory { Name = "Network" },
                    new ProblemCategory { Name = "Database" },
                    new ProblemCategory { Name = "Access / Login" }
                );
                await context.SaveChangesAsync();
            }

            return await context.ProblemCategories.ToListAsync();
        }

        private static async Task SeedProblemsAsync(ApplicationDbContext context, List<ProblemCategory> categories)
        {
            if (await context.Problems.AnyAsync())
            {
                return;
            }

            int CategoryId(string name) => categories.First(c => c.Name == name).Id;

            context.Problems.AddRange(
                new Problem { Name = "Barcode Scanner Not Working", ProblemCategoryId = CategoryId("Hardware") },
                new Problem { Name = "Barcode Scanning Slow", ProblemCategoryId = CategoryId("Hardware") },
                new Problem { Name = "Application Not Responding", ProblemCategoryId = CategoryId("Software / Application") },
                new Problem { Name = "MES Transaction Issue", ProblemCategoryId = CategoryId("Software / Application") },
                new Problem { Name = "Network Issue", ProblemCategoryId = CategoryId("Network") },
                new Problem { Name = "Printer Issue", ProblemCategoryId = CategoryId("Hardware") },
                new Problem { Name = "Database Issue", ProblemCategoryId = CategoryId("Database") },
                new Problem { Name = "Login Issue", ProblemCategoryId = CategoryId("Access / Login") },
                new Problem { Name = "Hardware Issue", ProblemCategoryId = CategoryId("Hardware") },
                new Problem { Name = "Performance Issue", ProblemCategoryId = CategoryId("Software / Application") }
            );
            await context.SaveChangesAsync();
        }

        private static async Task<List<Priority>> SeedPrioritiesAsync(ApplicationDbContext context)
        {
            if (!await context.Priorities.AnyAsync())
            {
                context.Priorities.AddRange(
                    new Priority { Name = "Low", Code = PriorityCodes.Low, Level = 1, ColorCode = "#6c757d" },
                    new Priority { Name = "Medium", Code = PriorityCodes.Medium, Level = 2, ColorCode = "#0d6efd" },
                    new Priority { Name = "High", Code = PriorityCodes.High, Level = 3, ColorCode = "#fd7e14" },
                    new Priority { Name = "Critical", Code = PriorityCodes.Critical, Level = 4, ColorCode = "#dc3545" }
                );
                await context.SaveChangesAsync();
            }

            return await context.Priorities.ToListAsync();
        }

        private static async Task SeedStatusesAsync(ApplicationDbContext context)
        {
            if (!await context.Statuses.AnyAsync())
            {
                context.Statuses.AddRange(
                    new Status { Name = "Open", Code = CallStatusCodes.Open, SortOrder = 1, ColorCode = "#0d6efd" },
                    new Status { Name = "Assigned", Code = CallStatusCodes.Assigned, SortOrder = 2, ColorCode = "#6610f2" },
                    new Status { Name = "In Progress", Code = CallStatusCodes.InProgress, SortOrder = 3, ColorCode = "#fd7e14" },
                    new Status { Name = "On Hold", Code = CallStatusCodes.OnHold, SortOrder = 4, ColorCode = "#6c757d" },
                    new Status { Name = "Resolved", Code = CallStatusCodes.Resolved, SortOrder = 5, ColorCode = "#20c997" },
                    new Status { Name = "Closed", Code = CallStatusCodes.Closed, SortOrder = 6, ColorCode = "#198754" },
                    new Status { Name = "Reopened", Code = CallStatusCodes.Reopened, SortOrder = 7, ColorCode = "#dc3545" },
                    new Status { Name = "Cancelled", Code = CallStatusCodes.Cancelled, SortOrder = 8, ColorCode = "#343a40" }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSLAConfigurationsAsync(ApplicationDbContext context, List<Priority> priorities)
        {
            if (await context.SLAConfigurations.AnyAsync())
            {
                return;
            }

            int PriorityId(string code) => priorities.First(p => p.Code == code).Id;

            // Section 28
            context.SLAConfigurations.AddRange(
                new SLAConfiguration { PriorityId = PriorityId(PriorityCodes.Critical), ResponseMinutes = 5, ResolutionMinutes = 30 },
                new SLAConfiguration { PriorityId = PriorityId(PriorityCodes.High), ResponseMinutes = 10, ResolutionMinutes = 60 },
                new SLAConfiguration { PriorityId = PriorityId(PriorityCodes.Medium), ResponseMinutes = 30, ResolutionMinutes = 240 },
                new SLAConfiguration { PriorityId = PriorityId(PriorityCodes.Low), ResponseMinutes = 60, ResolutionMinutes = 480 }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedEmployeesAsync(ApplicationDbContext context, List<Location> locations, List<Shop> shops)
        {
            if (await context.Employees.AnyAsync())
            {
                return;
            }

            var plant1 = locations.First(l => l.Code == "NSK1");
            var assembly = shops.First(s => s.LocationId == plant1.Id && s.Code == "ASSY");
            var paint = shops.First(s => s.LocationId == plant1.Id && s.Code == "PAINT");
            var quality = shops.First(s => s.LocationId == plant1.Id && s.Code == "QLTY");

            context.Employees.AddRange(
                new Employee { EmployeeCode = "EMP045", FullName = "Operator - EMP045", Designation = "Machine Operator", LocationId = plant1.Id, ShopId = assembly.Id },
                new Employee { EmployeeCode = "EMP046", FullName = "Ramesh Pawar", Designation = "Machine Operator", LocationId = plant1.Id, ShopId = assembly.Id },
                new Employee { EmployeeCode = "EMP047", FullName = "Suresh Jadhav", Designation = "Shift Supervisor", LocationId = plant1.Id, ShopId = paint.Id },
                new Employee { EmployeeCode = "EMP048", FullName = "Anita Kulkarni", Designation = "Quality Inspector", LocationId = plant1.Id, ShopId = quality.Id }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedUsersAndEngineersAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, List<Location> locations)
        {
            var plant1 = locations.First(l => l.Code == "NSK1");

            var seedUsers = new (string EmployeeId, string FullName, string Designation, string Role, bool IsEngineer)[]
            {
                ("ADMIN001", "Admin User", "System Administrator", RoleNames.Admin, false),
                ("EMP001", "Pranav Darandale", "Support Engineer", RoleNames.SupportEngineer, true),
                ("EMP002", "Abhijeet Kale", "Support Engineer", RoleNames.SupportEngineer, true),
                ("MGR001", "Support Manager", "Support Manager", RoleNames.SupportManager, true),
                ("SUP001", "Supervisor User", "Supervisor", RoleNames.Supervisor, true),
                ("VIEW001", "Viewer User", "Viewer", RoleNames.Viewer, false)
            };

            foreach (var seed in seedUsers)
            {
                var existing = await userManager.FindByNameAsync(seed.EmployeeId);
                if (existing != null)
                {
                    continue;
                }

                var user = new ApplicationUser
                {
                    UserName = seed.EmployeeId,
                    EmployeeId = seed.EmployeeId,
                    FullName = seed.FullName,
                    Designation = seed.Designation,
                    LocationId = plant1.Id,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, SeedPassword);
                if (!result.Succeeded)
                {
                    continue;
                }

                await userManager.AddToRoleAsync(user, seed.Role);

                if (seed.IsEngineer)
                {
                    context.Engineers.Add(new Engineer
                    {
                        ApplicationUserId = user.Id,
                        LocationId = plant1.Id
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
