namespace CallLogManagementSystem.ViewModels.Masters
{
    public class EmployeeRowVm
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string? ShopName { get; set; }
        public bool IsActive { get; set; }
    }
}
