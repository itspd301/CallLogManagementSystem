namespace CallLogManagementSystem.Interfaces
{
    public class LifecycleResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }

        public static LifecycleResult Success() => new() { Succeeded = true };
        public static LifecycleResult Failure(string error) => new() { Succeeded = false, Error = error };
    }

    // Section 8 — the core workflow transitions. On Hold and Cancel are deliberately not
    // covered here yet (both are legitimate but optional/edge branches of the lifecycle
    // diagram); this covers the main path: Assigned -> In Progress -> Resolved -> Closed,
    // plus Closed -> Reopened.
    public interface ICallLifecycleService
    {
        Task<LifecycleResult> StartProgressAsync(int callLogId, string performedByUserId);
        Task<LifecycleResult> ResolveAsync(int callLogId, string icaPca, string? remarks, string performedByUserId);
        Task<LifecycleResult> CloseAsync(int callLogId, string? remarks, string performedByUserId);
        Task<LifecycleResult> ReopenAsync(int callLogId, string reason, string performedByUserId);
    }
}
