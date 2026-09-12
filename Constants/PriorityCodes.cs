namespace CallLogManagementSystem.Constants
{
    // Same rationale as StatusCodes — Priority is an admin-configurable Master,
    // these codes are the stable identifiers SLA lookups key off.
    public static class PriorityCodes
    {
        public const string Low = "LOW";
        public const string Medium = "MEDIUM";
        public const string High = "HIGH";
        public const string Critical = "CRITICAL";
    }
}
