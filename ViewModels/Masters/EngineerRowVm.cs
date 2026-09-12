namespace CallLogManagementSystem.ViewModels.Masters
{
    public class EngineerRowVm
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? LocationName { get; set; }
        public bool IsActive { get; set; }
    }
}
