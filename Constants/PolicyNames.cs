namespace CallLogManagementSystem.Constants
{
    // Named authorization policies encoding the Section 7 permission matrix. Every controller
    // built from Phase 9 onward attributes its actions with [Authorize(Policy = PolicyNames.X)]
    // rather than hardcoding role lists, so the matrix lives in exactly one place (Program.cs).
    //
    // Deliberately NOT covered here: resource-based checks that depend on a specific record's
    // state (e.g. "can this engineer edit THIS call because it's assigned to them", "is this
    // call closed and therefore read-only"). Section 17/18 make clear those depend on runtime
    // data (call status, assignment) a static role policy can't see — they're enforced
    // imperatively inside the relevant service/controller once CallLog exists (Phase 14+).
    public static class PolicyNames
    {
        // Administration (Section 7 — Admin only)
        public const string ManageUsers = "ManageUsers";
        public const string ManageRoles = "ManageRoles";
        public const string ViewAuditLogs = "ViewAuditLogs";

        // Master Configuration (Admin full access; Support Manager reaches the area too —
        // which specific masters they may edit is a per-master, Phase 25 decision)
        public const string AccessMasterConfiguration = "AccessMasterConfiguration";

        // Call Management
        public const string ViewAllCalls = "ViewAllCalls";               // Admin, Support Manager, Supervisor, Viewer
        public const string CreateCall = "CreateCall";                   // Admin, Support Manager, Support Engineer
        public const string EditCall = "EditCall";                       // Admin, Support Manager
        public const string EditHistoricalCall = "EditHistoricalCall";   // Admin, Support Manager — editing a Closed call
        public const string AssignEngineer = "AssignEngineer";           // Admin, Support Manager
        public const string HandoverCall = "HandoverCall";               // Admin, Support Manager, Support Engineer
        public const string CloseCall = "CloseCall";                     // Admin, Support Manager, Supervisor
        public const string ResolveCall = "ResolveCall";                 // Admin, Support Manager, Support Engineer
        public const string AddCommentOrAttachment = "AddCommentOrAttachment"; // Everyone except Viewer

        // Reports
        public const string ViewReports = "ViewReports"; // Admin, Support Manager, Supervisor, Viewer
    }
}
