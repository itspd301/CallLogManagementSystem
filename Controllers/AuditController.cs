using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Models.Enums;
using CallLogManagementSystem.ViewModels.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    // Section 29 — accessible only to authorized (Admin) users.
    [Authorize(Policy = PolicyNames.ViewAuditLogs)]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search, AuditAction? auditAction, string? entityName, DateTime? dateFrom, DateTime? dateTo,
            int page = 1, int pageSize = 50)
        {
            // Parameter deliberately NOT named "action" — that's a reserved MVC routing token
            // (the current action method's own name), and binding an enum parameter to it
            // silently fails since the route's "action" value is the string "Index".
            var query = _context.AuditLogs.AsNoTracking()
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    EF.Functions.Like(a.EntityId ?? "", $"%{term}%") ||
                    EF.Functions.Like(a.OldValue ?? "", $"%{term}%") ||
                    EF.Functions.Like(a.NewValue ?? "", $"%{term}%") ||
                    (a.User != null && EF.Functions.Like(a.User.FullName, $"%{term}%")));
            }

            if (auditAction.HasValue) query = query.Where(a => a.Action == auditAction);
            if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);
            if (dateFrom.HasValue) query = query.Where(a => a.Timestamp >= dateFrom.Value.Date);
            if (dateTo.HasValue) query = query.Where(a => a.Timestamp < dateTo.Value.Date.AddDays(1));

            query = query.OrderByDescending(a => a.Timestamp);

            page = Math.Max(1, page);
            pageSize = pageSize is > 0 and <= 500 ? pageSize : 50;

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogRowVm
                {
                    Id = a.Id,
                    UserName = a.User != null ? a.User.FullName : null,
                    EmployeeId = a.User != null ? a.User.EmployeeId : null,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    Timestamp = a.Timestamp,
                    IPAddress = a.IPAddress
                })
                .ToListAsync();

            var entityNameOptions = await _context.AuditLogs.AsNoTracking()
                .Select(a => a.EntityName)
                .Distinct()
                .OrderBy(n => n)
                .Select(n => new SelectListItem { Value = n, Text = n })
                .ToListAsync();

            var vm = new AuditLogListViewModel
            {
                Items = items,
                Search = search,
                Action = auditAction,
                EntityName = entityName,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                EntityNameOptions = entityNameOptions
            };

            return View(vm);
        }
    }
}
