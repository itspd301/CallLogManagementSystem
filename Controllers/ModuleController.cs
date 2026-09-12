using CallLogManagementSystem.Controllers.Masters;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Masters;

namespace CallLogManagementSystem.Controllers
{
    public class ModuleController : SimpleMasterControllerBase<Module>
    {
        public ModuleController(ApplicationDbContext context, IAuditService auditService) : base(context, auditService) { }
        protected override string EntityDisplayName => "Module";
    }
}
