using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    // Section 26 — every role reaches the dashboard; the underlying CallLog query respects
    // whatever "All Calls" scope Section 7 would otherwise grant them (Admin/Manager/Supervisor/
    // Viewer see plant-wide figures). No engineer-only narrowing here since a dashboard is a
    // read-only overview, not an action surface.
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private class LabelValue
        {
            public string Label { get; set; } = string.Empty;
            public double Value { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? dateFrom, DateTime? dateTo, int? locationId, int? shopId, int? moduleId, int? engineerId, int? priorityId)
        {
            // Default to the last 30 days so the day-trend chart and KPI cards stay meaningful
            // (and bounded) even before the user picks a range.
            var effectiveDateTo = dateTo?.Date ?? DateTime.Today;
            var effectiveDateFrom = dateFrom?.Date ?? effectiveDateTo.AddDays(-29);

            var query = _context.CallLogs.AsNoTracking()
                .Where(c => c.ReportedDateTime >= effectiveDateFrom && c.ReportedDateTime < effectiveDateTo.AddDays(1));

            if (locationId.HasValue) query = query.Where(c => c.LocationId == locationId);
            if (shopId.HasValue) query = query.Where(c => c.ShopId == shopId);
            if (moduleId.HasValue) query = query.Where(c => c.ModuleId == moduleId);
            if (engineerId.HasValue) query = query.Where(c => c.AttendedById == engineerId);
            if (priorityId.HasValue) query = query.Where(c => c.PriorityId == priorityId);

            // One shot: status code -> count, avoids eight separate COUNT round-trips.
            var statusCounts = await query
                .GroupBy(c => c.Status!.Code)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Code, g => g.Count);

            int CountOf(string code) => statusCounts.TryGetValue(code, out var n) ? n : 0;

            var totalCalls = statusCounts.Values.Sum();
            var criticalCount = await query.CountAsync(c => c.Priority!.Code == PriorityCodes.Critical);
            var lineLossCount = await query.CountAsync(c => c.IsLineLoss);

            var avgResolutionMinutes = await query
                .Where(c => c.TimeTakenMinutes != null)
                .Select(c => (double?)c.TimeTakenMinutes)
                .AverageAsync();

            // SLA breach — evaluated in memory over this (date-bounded) slice: open calls
            // breach if they've already run longer than their priority's resolution target;
            // closed calls breach if their recorded Time Taken exceeded it.
            var slaLookup = await _context.SLAConfigurations.AsNoTracking()
                .Where(s => s.IsActive)
                .ToDictionaryAsync(s => s.PriorityId, s => s.ResolutionMinutes);

            var slaEvalRows = await query
                .Select(c => new { c.PriorityId, StatusCode = c.Status!.Code, c.ReportedDateTime, c.TimeTakenMinutes })
                .ToListAsync();

            var now = DateTime.Now;
            var slaBreachedCount = slaEvalRows.Count(c =>
            {
                if (!slaLookup.TryGetValue(c.PriorityId, out var resolutionTarget))
                {
                    return false;
                }

                return c.StatusCode == CallStatusCodes.Closed
                    ? c.TimeTakenMinutes.HasValue && c.TimeTakenMinutes.Value > resolutionTarget
                    : (now - c.ReportedDateTime).TotalMinutes > resolutionTarget;
            });

            // Each chart is its own directly-translatable GroupBy+aggregate query (Count/Sum/
            // Average inside the Select projection) — never materializing raw IGrouping<,>
            // entities, which EF Core's SQL provider cannot translate.
            var callsByLocation = await ToChartAsync(query.GroupBy(c => c.Location!.Name)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Count() }));

            var callsByShop = await ToChartAsync(query.GroupBy(c => c.Shop!.Name)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Count() }));

            var callsByModule = await ToChartAsync(query.GroupBy(c => c.Module!.Name)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Count() }));

            var callsByProblemCategory = await ToChartAsync(query.GroupBy(c => c.ProblemCategory!.Name)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Count() }));

            var callsByEngineer = await ToChartAsync(query.GroupBy(c => c.AttendedBy!.ApplicationUser!.FullName)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Count() }));

            var callsByPriority = await ToChartAsync(query.GroupBy(c => c.Priority!.Name)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Count() }));

            var callsByCallType = await ToChartAsync(query
                .GroupBy(c => c.CallType)
                .Select(g => new LabelValue { Label = g.Key == Models.Enums.CallType.UserSupport ? "User Support" : "Shopfloor Concern", Value = g.Count() }));

            var lineLossByShop = await ToChartAsync(query.Where(c => c.IsLineLoss).GroupBy(c => c.Shop!.Name)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Sum(c => c.LineLossDurationMinutes ?? 0) }));

            var resolutionTimeByPriority = await ToChartAsync(query.Where(c => c.TimeTakenMinutes != null).GroupBy(c => c.Priority!.Name)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Average(c => (double?)c.TimeTakenMinutes) ?? 0 }));

            var model = new DashboardViewModel
            {
                TotalCalls = totalCalls,
                OpenCount = CountOf(CallStatusCodes.Open),
                AssignedCount = CountOf(CallStatusCodes.Assigned),
                InProgressCount = CountOf(CallStatusCodes.InProgress),
                OnHoldCount = CountOf(CallStatusCodes.OnHold),
                ResolvedCount = CountOf(CallStatusCodes.Resolved),
                ClosedCount = CountOf(CallStatusCodes.Closed),
                ReopenedCount = CountOf(CallStatusCodes.Reopened),
                CriticalCount = criticalCount,
                LineLossCount = lineLossCount,
                SlaBreachedCount = slaBreachedCount,
                AverageResolutionMinutes = avgResolutionMinutes,

                CallsByDay = await BuildCallsByDayAsync(query, effectiveDateFrom, effectiveDateTo),
                CallsByLocation = callsByLocation,
                CallsByShop = callsByShop,
                CallsByModule = callsByModule,
                CallsByProblemCategory = callsByProblemCategory,
                CallsByEngineer = callsByEngineer,
                CallsByPriority = callsByPriority,
                CallsByCallType = callsByCallType,
                LineLossByShop = lineLossByShop,
                ResolutionTimeByPriority = resolutionTimeByPriority,

                OpenVsClosed = new ChartDataVm
                {
                    Labels = new List<string> { "Open (active)", "Closed" },
                    Data = new List<double> { totalCalls - CountOf(CallStatusCodes.Closed), CountOf(CallStatusCodes.Closed) }
                },

                DateFrom = effectiveDateFrom,
                DateTo = effectiveDateTo,
                LocationId = locationId,
                ShopId = shopId,
                ModuleId = moduleId,
                EngineerId = engineerId,
                PriorityId = priorityId,

                LocationOptions = await _context.Locations.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Name)
                    .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync(),
                ModuleOptions = await _context.Modules.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.Name)
                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name }).ToListAsync(),
                EngineerOptions = await _context.Engineers.AsNoTracking().Where(e => e.IsActive).Include(e => e.ApplicationUser)
                    .OrderBy(e => e.ApplicationUser!.FullName)
                    .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.ApplicationUser!.FullName }).ToListAsync(),
                PriorityOptions = await _context.Priorities.AsNoTracking().Where(p => p.IsActive).OrderByDescending(p => p.Level)
                    .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name }).ToListAsync()
            };

            return View(model);
        }

        private static async Task<ChartDataVm> ToChartAsync(IQueryable<LabelValue> aggregatedQuery)
        {
            var rows = await aggregatedQuery.ToListAsync();
            var ordered = rows.OrderByDescending(r => r.Value).Take(12).ToList();
            return new ChartDataVm
            {
                Labels = ordered.Select(r => r.Label).ToList(),
                Data = ordered.Select(r => Math.Round(r.Value, 1)).ToList()
            };
        }

        private static async Task<ChartDataVm> BuildCallsByDayAsync(IQueryable<CallLog> query, DateTime from, DateTime to)
        {
            var raw = await query
                .GroupBy(c => c.ReportedDateTime.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            var byDay = raw.ToDictionary(r => r.Day, r => r.Count);
            var labels = new List<string>();
            var data = new List<double>();

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                labels.Add(day.ToString("dd-MMM"));
                data.Add(byDay.TryGetValue(day, out var c) ? c : 0);
            }

            return new ChartDataVm { Labels = labels, Data = data };
        }
    }
}
