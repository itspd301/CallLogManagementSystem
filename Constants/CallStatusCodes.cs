namespace CallLogManagementSystem.Constants
{
    // Statuses are an admin-configurable Master (Section 25), so display names/order can change.
    // These codes are the stable identifiers business logic (SLA, edit permissions, workflow
    // transitions) is allowed to hardcode against — set on Status.Code and never renamed.
    // Named CallStatusCodes (not StatusCodes) to avoid colliding with the ASP.NET Core
    // Microsoft.AspNetCore.Http.StatusCodes class pulled in via implicit usings.
    public static class CallStatusCodes
    {
        public const string Open = "OPEN";
        public const string Assigned = "ASSIGNED";
        public const string InProgress = "IN_PROGRESS";
        public const string OnHold = "ON_HOLD";
        public const string Resolved = "RESOLVED";
        public const string Closed = "CLOSED";
        public const string Reopened = "REOPENED";
        public const string Cancelled = "CANCELLED";
    }
}
