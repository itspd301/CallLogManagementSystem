namespace CallLogManagementSystem.Interfaces
{
    public class HandoverResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }

        public static HandoverResult Success() => new() { Succeeded = true };
        public static HandoverResult Failure(string error) => new() { Succeeded = false, Error = error };
    }

    // Section 21 — engineer-to-engineer handover with a required reason, distinct from
    // Assignment (Phase 16, manager-directed, no reason required). FromEngineer is always the
    // call's current AttendedBy — callers don't get to pick who the call is "from".
    public interface IHandoverService
    {
        Task<HandoverResult> HandoverAsync(int callLogId, int toEngineerId, string reason, string? remarks, string performedByUserId);
    }
}
