namespace CallLogManagementSystem.ViewModels.Notifications
{
    public class NotificationRowVm
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CallLogId { get; set; }
        public string CallNumber { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
