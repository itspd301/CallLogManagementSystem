namespace CallLogManagementSystem.ViewModels.Masters
{
    public class SLAConfigurationRowVm
    {
        public int PriorityId { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public string? PriorityColor { get; set; }
        public bool HasConfiguration { get; set; }
        public int? ResponseMinutes { get; set; }
        public int? ResolutionMinutes { get; set; }
        public bool IsActive { get; set; }
    }
}
