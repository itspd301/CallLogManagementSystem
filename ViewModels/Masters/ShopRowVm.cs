namespace CallLogManagementSystem.ViewModels.Masters
{
    public class ShopRowVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
