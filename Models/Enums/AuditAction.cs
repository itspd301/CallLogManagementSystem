namespace CallLogManagementSystem.Models.Enums
{
    // Section 29 — system-wide security/audit trail, broader than the per-call ActivityType
    // (also covers logins, master data changes, and user/role changes).
    public enum AuditAction
    {
        Login = 1,
        LoginFailed = 2,
        Logout = 3,
        CallCreated = 4,
        CallEdited = 5,
        StatusChanged = 6,
        EngineerChanged = 7,
        PriorityChanged = 8,
        CallClosed = 9,
        CallReopened = 10,
        MasterChanged = 11,
        UserChanged = 12
    }
}
