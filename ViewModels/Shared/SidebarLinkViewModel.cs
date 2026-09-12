namespace CallLogManagementSystem.ViewModels.Shared
{
    // Not a persisted/domain model — pure UI data for rendering one _SidebarLink partial.
    public class SidebarLinkViewModel
    {
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = "Index";
        public Dictionary<string, string>? RouteValues { get; set; }
        public string Icon { get; set; } = "list";
        public string Label { get; set; } = string.Empty;
    }
}
