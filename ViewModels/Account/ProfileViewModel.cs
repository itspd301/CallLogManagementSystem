namespace CallLogManagementSystem.ViewModels.Account
{
    public class ProfileViewModel
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? LocationName { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
