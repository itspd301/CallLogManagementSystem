namespace CallLogManagementSystem.Interfaces
{
    // Thin accessor over the signed-in user's claims, used wherever the app needs to know
    // "who is logged in right now" — audit logging (Phase 7), the navbar identity block
    // (Phase 9), and auto-populating Created By / Attended By on Call creation (Phase 11).
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        string? UserId { get; }
        string? EmployeeId { get; }
        string? FullName { get; }
        string? Designation { get; }
        string? IPAddress { get; }
        bool IsInRole(string role);

        // Highest-priority role the user holds (Admin > Support Manager > Support Engineer >
        // Supervisor > Viewer), for the single-line role label shown in the navbar. A user is
        // expected to hold exactly one role, but this stays well-defined even if that changes.
        string? PrimaryRole { get; }
    }
}
