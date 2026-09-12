namespace CallLogManagementSystem.Models.Enums
{
    // Fixed by business rule (Section 9, Field 4) — not admin-configurable, unlike
    // CallCategory/ProblemCategory which live in Master Configuration.
    public enum CallType
    {
        UserSupport = 1,
        ShopfloorConcern = 2
    }
}
