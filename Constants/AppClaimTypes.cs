namespace CallLogManagementSystem.Constants
{
    // Custom claims baked into the auth cookie at sign-in (see ApplicationUserClaimsPrincipalFactory)
    // so the navbar/audit trail never needs an extra DB round-trip just to show who's logged in.
    public static class AppClaimTypes
    {
        public const string FullName = "FullName";
        public const string Designation = "Designation";
    }
}
