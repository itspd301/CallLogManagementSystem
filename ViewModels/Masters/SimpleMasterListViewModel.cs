namespace CallLogManagementSystem.ViewModels.Masters
{
    public class SimpleMasterRowVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }

    // Shared by every Group A master (Location, Module, ApplicationType, CallCategory,
    // ProblemCategory) so one Index view serves all five controllers.
    public class SimpleMasterListViewModel
    {
        public string EntityDisplayName { get; set; } = string.Empty;
        public string ControllerName { get; set; } = string.Empty;
        public bool HasCode { get; set; }

        public List<SimpleMasterRowVm> Items { get; set; } = new();

        public string? Search { get; set; }
        public string Sort { get; set; } = "Name";
        public string Dir { get; set; } = "asc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
