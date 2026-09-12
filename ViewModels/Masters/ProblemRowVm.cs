namespace CallLogManagementSystem.ViewModels.Masters
{
    public class ProblemRowVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProblemCategoryName { get; set; } = string.Empty;
        public string? ModuleName { get; set; }
        public bool IsActive { get; set; }
    }
}
