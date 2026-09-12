namespace CallLogManagementSystem.Interfaces
{
    // Implemented by every Master entity shaped as Name(+Code)+IsActive, so the generic
    // SimpleMasterControllerBase can list/search/sort/create/edit them without reflection.
    public interface INamedMasterEntity
    {
        int Id { get; set; }
        string Name { get; set; }
        bool IsActive { get; set; }
    }

    public interface ICodedMasterEntity : INamedMasterEntity
    {
        string Code { get; set; }
    }
}
