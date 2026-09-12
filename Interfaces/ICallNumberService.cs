namespace CallLogManagementSystem.Interfaces
{
    public interface ICallNumberService
    {
        // Returns a new, guaranteed-unique CALL-YYYY-NNNNNN number, safe under concurrent callers.
        Task<string> GenerateAsync();
    }
}
