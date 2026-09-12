using CallLogManagementSystem.Controllers.Masters;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Masters;

namespace CallLogManagementSystem.Controllers
{
    public class ApplicationTypeController : SimpleMasterControllerBase<ApplicationType>
    {
        public ApplicationTypeController(ApplicationDbContext context, IAuditService auditService) : base(context, auditService) { }
        protected override string EntityDisplayName => "Application Type";
    }
}
