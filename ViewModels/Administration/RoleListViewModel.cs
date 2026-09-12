namespace CallLogManagementSystem.ViewModels.Administration
{
    public class RoleRowVm
    {
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; }
    }

    public class RoleListViewModel
    {
        public List<RoleRowVm> Roles { get; set; } = new();
    }
}
