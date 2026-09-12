namespace CallLogManagementSystem.Constants
{
    // Section 7 — the five fixed application roles, seeded as IdentityRole rows in Phase 6
    // and referenced by [Authorize(Roles = ...)] starting Phase 8.
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string SupportManager = "Support Manager";
        public const string SupportEngineer = "Support Engineer";
        public const string Supervisor = "Supervisor";
        public const string Viewer = "Viewer";
    }
}
