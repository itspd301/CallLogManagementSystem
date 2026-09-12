using CallLogManagementSystem.Controllers.Masters;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.Masters;

namespace CallLogManagementSystem.Controllers
{
    public class ProblemCategoryController : SimpleMasterControllerBase<ProblemCategory>
    {
        public ProblemCategoryController(ApplicationDbContext context, IAuditService auditService) : base(context, auditService) { }
        protected override string EntityDisplayName => "Problem Category";
    }
}
