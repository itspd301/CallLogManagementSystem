namespace CallLogManagementSystem.Interfaces
{
    public class AssignmentResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }

        public static AssignmentResult Success() => new() { Succeeded = true };
        public static AssignmentResult Failure(string error) => new() { Succeeded = false, Error = error };
    }

    // Section 20 — assigning/reassigning the engineer responsible for a call. Distinct from
    // Handover (Phase 17, engineer-to-engineer with a reason) and from just editing AttendedBy
    // via Edit Previous Entry: assigning also advances the call's workflow status (Section 8:
    // Open -> Assigned) and keeps a full CallAssignment history, not just the current value.
    public interface IAssignmentService
    {
        Task<AssignmentResult> AssignAsync(int callLogId, int engineerId, string? remarks, string performedByUserId);
    }
}
