namespace CallLogManagementSystem.Models.Enums
{
    // Section 19 — every entry drawn from this list appears on the Call's Activity Timeline.
    public enum ActivityType
    {
        CallCreated = 1,
        CallEdited = 2,
        FieldUpdated = 3,
        StatusChanged = 4,
        EngineerAssigned = 5,
        EngineerChanged = 6,
        CallHandedOver = 7,
        CommentAdded = 8,
        AttachmentAdded = 9,
        ICAPCAUpdated = 10,
        LineLossUpdated = 11,
        CallResolved = 12,
        CallClosed = 13,
        CallReopened = 14,
        AttachmentRemoved = 15
    }
}
