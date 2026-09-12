using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.ViewModels.CallLog;

namespace CallLogManagementSystem.Interfaces
{
    public class CallLogCreateResult
    {
        public bool Succeeded { get; set; }
        public CallLog? CallLog { get; set; }
        public Dictionary<string, string> Errors { get; set; } = new();

        public static CallLogCreateResult Success(CallLog callLog) => new() { Succeeded = true, CallLog = callLog };
        public static CallLogCreateResult Failure(string field, string message) =>
            new() { Succeeded = false, Errors = new Dictionary<string, string> { [field] = message } };
    }

    public class CallLogUpdateResult
    {
        public bool Succeeded { get; set; }
        public Dictionary<string, string> Errors { get; set; } = new();
        public List<string> Changes { get; set; } = new();

        public static CallLogUpdateResult Success(List<string> changes) => new() { Succeeded = true, Changes = changes };
        public static CallLogUpdateResult Failure(string field, string message) =>
            new() { Succeeded = false, Errors = new Dictionary<string, string> { [field] = message } };
    }

    public interface ICallLogService
    {
        // Assumes the caller (controller) has already resolved role-driven overrides
        // (AttendedById / ReportedDateTime) onto the model — this validates and persists
        // pure domain rules (Section 34) that apply regardless of who's creating the call.
        Task<CallLogCreateResult> CreateAsync(CreateCallLogViewModel model, string createdByUserId);

        Task<CallLog?> GetByIdAsync(int id);

        // Applies only the fields the caller's permission tier allows (canEditSpecialFields
        // gates Location/Shop/Module/ApplicationType/ReportedBy/ReportedDateTime/AttendedBy/
        // Line Loss per Section 17); anything else in the model is silently ignored rather than
        // rejecting the whole request, since the view already disables those inputs for this user.
        // Writes one CallActivity per field that actually changed (Section 16) and returns a
        // human-readable summary of the changes for the "show changes" requirement.
        Task<CallLogUpdateResult> UpdateAsync(int id, ViewModels.CallLog.EditCallLogViewModel model, string performedByUserId, bool canEditSpecialFields);
    }
}
