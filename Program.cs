using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Identity;
using CallLogManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Data.SqlClient; // for direct connection test

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Serilog (structured logging, rolling file sink, read from appsettings)
// ---------------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ---------------------------------------------------------------------
// Database (SQL Server via EF Core)
// ---------------------------------------------------------------------
// ---------------------------------------------------------------------
// Database (SQL Server via EF Core)
// ---------------------------------------------------------------------
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
Log.Information("Resolved connection string: {ConnStr}", connStr);

// Quick direct connection test before EF Core migrations
try
{
    using var testConn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
    testConn.Open();
    Log.Information("Direct SQL connection successful!");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Direct SQL connection failed!");
    throw;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connStr));


// ---------------------------------------------------------------------
// ASP.NET Core Identity (custom ApplicationUser: EmployeeId, FullName, etc.)
// ---------------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
});

builder.Services.AddHttpContextAccessor();

// ---------------------------------------------------------------------
// Authorization policies
// ---------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.ManageUsers, p => p.RequireRole(RoleNames.Admin));
    options.AddPolicy(PolicyNames.ManageRoles, p => p.RequireRole(RoleNames.Admin));
    options.AddPolicy(PolicyNames.ViewAuditLogs, p => p.RequireRole(RoleNames.Admin));
    options.AddPolicy(PolicyNames.AccessMasterConfiguration, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager));
    options.AddPolicy(PolicyNames.ViewAllCalls, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager, RoleNames.Supervisor, RoleNames.Viewer));
    options.AddPolicy(PolicyNames.CreateCall, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager, RoleNames.SupportEngineer));
    options.AddPolicy(PolicyNames.EditCall, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager));
    options.AddPolicy(PolicyNames.EditHistoricalCall, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager));
    options.AddPolicy(PolicyNames.AssignEngineer, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager));
    options.AddPolicy(PolicyNames.HandoverCall, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager, RoleNames.SupportEngineer));
    options.AddPolicy(PolicyNames.CloseCall, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager, RoleNames.Supervisor));
    options.AddPolicy(PolicyNames.ResolveCall, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager, RoleNames.SupportEngineer));
    options.AddPolicy(PolicyNames.AddCommentOrAttachment, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager, RoleNames.SupportEngineer, RoleNames.Supervisor));
    options.AddPolicy(PolicyNames.ViewReports, p =>
        p.RequireRole(RoleNames.Admin, RoleNames.SupportManager, RoleNames.Supervisor, RoleNames.Viewer));
});

// ---------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICallNumberService, CallNumberService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<ICallLogService, CallLogService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IHandoverService, HandoverService>();
builder.Services.AddScoped<ICallLifecycleService, CallLifecycleService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// ---------------------------------------------------------------------
// Apply pending migrations and seed reference/demo data on startup.
// ---------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await CallLogManagementSystem.Data.DbSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Database migration/seed failed on startup. Is SQL Server reachable and is the connection string in appsettings.json correct?");
        throw;
    }
}

// ---------------------------------------------------------------------
// HTTP request pipeline
// ---------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
