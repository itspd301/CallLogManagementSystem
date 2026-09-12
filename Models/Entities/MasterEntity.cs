namespace CallLogManagementSystem.Models.Entities
{
    // Common shape for all Master Configuration tables (Section 25):
    // every master supports Add/Edit/View/Activate/Deactivate and is never hard-deleted.
    public abstract class MasterEntity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
