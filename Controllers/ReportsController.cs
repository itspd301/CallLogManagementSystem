using CallLogManagementSystem.Constants;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Models.Entities.CallManagement;
using CallLogManagementSystem.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CallLogManagementSystem.Controllers
{
    // Section 24/27/28 — Admin, Support Manager, Supervisor, Viewer (matches ViewReports policy).
    [Authorize(Policy = PolicyNames.ViewReports)]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------------
        // Call Reports — consolidates Section 39's Daily/Monthly/Open/Closed/Location-wise/
        // Shop-wise/Module-wise/Problem-wise reports into one flexible grouped summary rather
        // than eight near-identical pages.
        // ------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> CallReport(
            DateTime? dateFrom, DateTime? dateTo, int? locationId, int? shopId, int? moduleId, int? statusId, int? priorityId,
            ReportGroupBy groupBy = ReportGroupBy.Day)
        {
            var model = await BuildCallReportModelAsync(dateFrom, dateTo, locationId, shopId, moduleId, statusId, priorityId, groupBy);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CallReportExportExcel(
            DateTime? dateFrom, DateTime? dateTo, int? locationId, int? shopId, int? moduleId, int? statusId, int? priorityId,
            ReportGroupBy groupBy = ReportGroupBy.Day)
        {
            var model = await BuildCallReportModelAsync(dateFrom, dateTo, locationId, shopId, moduleId, statusId, priorityId, groupBy);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Call Report");

            sheet.Cell(1, 1).Value = "Call Report";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Cell(2, 1).Value = $"{model.DateFrom:dd-MMM-yyyy} to {model.DateTo:dd-MMM-yyyy}  |  Grouped by {model.GroupBy}";
            sheet.Cell(2, 1).Style.Font.Italic = true;

            var headerRow = 4;
            string[] headers = { model.GroupBy.ToString(), "Total", "Open", "Closed", "Line Loss", "Avg Resolution (min)" };
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#e9ecef");
                cell.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            }

            var row = headerRow + 1;
            foreach (var r in model.Rows)
            {
                sheet.Cell(row, 1).Value = r.GroupLabel;
                sheet.Cell(row, 2).Value = r.Total;
                sheet.Cell(row, 3).Value = r.OpenCount;
                sheet.Cell(row, 4).Value = r.ClosedCount;
                sheet.Cell(row, 5).Value = r.LineLossCount;
                sheet.Cell(row, 6).Value = r.AverageResolutionMinutes.HasValue ? Math.Round(r.AverageResolutionMinutes.Value, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
                row++;
            }

            sheet.Cell(row, 1).Value = "Grand Total";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = model.GrandTotal;
            sheet.Cell(row, 2).Style.Font.Bold = true;
            sheet.Cell(row, 3).Value = model.GrandOpen;
            sheet.Cell(row, 3).Style.Font.Bold = true;
            sheet.Cell(row, 4).Value = model.GrandClosed;
            sheet.Cell(row, 4).Style.Font.Bold = true;
            sheet.Cell(row, 5).Value = model.GrandLineLoss;
            sheet.Cell(row, 5).Style.Font.Bold = true;

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"CallReport_{model.DateFrom:yyyyMMdd}_{model.DateTo:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> CallReportExportPdf(
            DateTime? dateFrom, DateTime? dateTo, int? locationId, int? shopId, int? moduleId, int? statusId, int? priorityId,
            ReportGroupBy groupBy = ReportGroupBy.Day)
        {
            var model = await BuildCallReportModelAsync(dateFrom, dateTo, locationId, shopId, moduleId, statusId, priorityId, groupBy);

            var document = new MigraDocCore.DocumentObjectModel.Document();
            var section = document.AddSection();
            section.PageSetup.Orientation = MigraDocCore.DocumentObjectModel.Orientation.Landscape;

            var title = section.AddParagraph("Call Report");
            title.Format.Font.Size = 16;
            title.Format.Font.Bold = true;

            var subtitle = section.AddParagraph($"{model.DateFrom:dd-MMM-yyyy} to {model.DateTo:dd-MMM-yyyy}  |  Grouped by {model.GroupBy}");
            subtitle.Format.Font.Italic = true;
            subtitle.Format.SpaceAfter = 12;

            var table = section.AddTable();
            table.Borders.Width = 0.5;
            string[] headers = { model.GroupBy.ToString(), "Total", "Open", "Closed", "Line Loss", "Avg Resolution" };
            table.AddColumn("6cm");
            for (var i = 1; i < headers.Length; i++)
            {
                table.AddColumn("3.5cm");
            }

            var headerRow = table.AddRow();
            headerRow.Shading.Color = MigraDocCore.DocumentObjectModel.Colors.LightGray;
            headerRow.Format.Font.Bold = true;
            for (var i = 0; i < headers.Length; i++)
            {
                headerRow.Cells[i].AddParagraph(headers[i]);
            }

            foreach (var r in model.Rows)
            {
                var dataRow = table.AddRow();
                dataRow.Cells[0].AddParagraph(r.GroupLabel);
                dataRow.Cells[1].AddParagraph(r.Total.ToString());
                dataRow.Cells[2].AddParagraph(r.OpenCount.ToString());
                dataRow.Cells[3].AddParagraph(r.ClosedCount.ToString());
                dataRow.Cells[4].AddParagraph(r.LineLossCount.ToString());
                dataRow.Cells[5].AddParagraph(r.AverageResolutionMinutes.HasValue ? $"{r.AverageResolutionMinutes.Value:0.#} min" : "-");
            }

            var totalRow = table.AddRow();
            totalRow.Format.Font.Bold = true;
            totalRow.Cells[0].AddParagraph("Grand Total");
            totalRow.Cells[1].AddParagraph(model.GrandTotal.ToString());
            totalRow.Cells[2].AddParagraph(model.GrandOpen.ToString());
            totalRow.Cells[3].AddParagraph(model.GrandClosed.ToString());
            totalRow.Cells[4].AddParagraph(model.GrandLineLoss.ToString());
            totalRow.Cells[5].AddParagraph(string.Empty);

            var renderer = new MigraDocCore.Rendering.PdfDocumentRenderer { Document = document };
            renderer.RenderDocument();

            using var stream = new MemoryStream();
            renderer.PdfDocument.Save(stream, false);
            var fileName = $"CallReport_{model.DateFrom:yyyyMMdd}_{model.DateTo:yyyyMMdd}.pdf";
            return File(stream.ToArray(), "application/pdf", fileName);
        }

        private async Task<CallReportViewModel> BuildCallReportModelAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId, int? shopId, int? moduleId, int? statusId, int? priorityId,
            ReportGroupBy groupBy)
        {
            var effectiveDateTo = dateTo?.Date ?? DateTime.Today;
            var effectiveDateFrom = dateFrom?.Date ?? effectiveDateTo.AddDays(-29);

            var query = _context.CallLogs.AsNoTracking()
                .Where(c => c.ReportedDateTime >= effectiveDateFrom && c.ReportedDateTime < effectiveDateTo.AddDays(1));

            if (locationId.HasValue) query = query.Where(c => c.LocationId == locationId);
            if (shopId.HasValue) query = query.Where(c => c.ShopId == shopId);
            if (moduleId.HasValue) query = query.Where(c => c.ModuleId == moduleId);
            if (statusId.HasValue) query = query.Where(c => c.StatusId == statusId);
            if (priorityId.HasValue) query = query.Where(c => c.PriorityId == priorityId);

            var rows = new List<ReportSummaryRowVm>();

            switch (groupBy)
            {
                case ReportGroupBy.Month:
                    var byMonth = await query
                        .GroupBy(c => new { c.ReportedDateTime.Year, c.ReportedDateTime.Month })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            Total = g.Count(),
                            Open = g.Count(c => c.Status!.Code != CallStatusCodes.Closed),
                            Closed = g.Count(c => c.Status!.Code == CallStatusCodes.Closed),
                            LineLoss = g.Count(c => c.IsLineLoss),
                            AvgMinutes = g.Average(c => (double?)c.TimeTakenMinutes)
                        })
                        .OrderBy(g => g.Year).ThenBy(g => g.Month)
                        .ToListAsync();
                    rows = byMonth.Select(g => new ReportSummaryRowVm
                    {
                        GroupLabel = new DateTime(g.Year, g.Month, 1).ToString("MMM yyyy"),
                        Total = g.Total, OpenCount = g.Open, ClosedCount = g.Closed, LineLossCount = g.LineLoss, AverageResolutionMinutes = g.AvgMinutes
                    }).ToList();
                    break;

                case ReportGroupBy.Location:
                    rows = await GroupedSummaryAsync(query, c => c.Location!.Name);
                    break;

                case ReportGroupBy.Shop:
                    rows = await GroupedSummaryAsync(query, c => c.Shop!.Name);
                    break;

                case ReportGroupBy.Module:
                    rows = await GroupedSummaryAsync(query, c => c.Module!.Name);
                    break;

                case ReportGroupBy.ProblemCategory:
                    rows = await GroupedSummaryAsync(query, c => c.ProblemCategory!.Name);
                    break;

                case ReportGroupBy.Status:
                    rows = await GroupedSummaryAsync(query, c => c.Status!.Name);
                    break;

                default: // Day
                    var byDay = await query
                        .GroupBy(c => c.ReportedDateTime.Date)
                        .Select(g => new
                        {
                            Day = g.Key,
                            Total = g.Count(),
                            Open = g.Count(c => c.Status!.Code != CallStatusCodes.Closed),
                            Closed = g.Count(c => c.Status!.Code == CallStatusCodes.Closed),
                            LineLoss = g.Count(c => c.IsLineLoss),
                            AvgMinutes = g.Average(c => (double?)c.TimeTakenMinutes)
                        })
                        .OrderBy(g => g.Day)
                        .ToListAsync();
                    rows = byDay.Select(g => new ReportSummaryRowVm
                    {
                        GroupLabel = g.Day.ToString("dd-MMM-yyyy"),
                        Total = g.Total, OpenCount = g.Open, ClosedCount = g.Closed, LineLossCount = g.LineLoss, AverageResolutionMinutes = g.AvgMinutes
                    }).ToList();
                    break;
            }

            var model = new CallReportViewModel
            {
                Rows = rows,
                DateFrom = effectiveDateFrom,
                DateTo = effectiveDateTo,
                LocationId = locationId,
                ShopId = shopId,
                ModuleId = moduleId,
                StatusId = statusId,
                PriorityId = priorityId,
                GroupBy = groupBy,
                GrandTotal = rows.Sum(r => r.Total),
                GrandOpen = rows.Sum(r => r.OpenCount),
                GrandClosed = rows.Sum(r => r.ClosedCount),
                GrandLineLoss = rows.Sum(r => r.LineLossCount),
                LocationOptions = await OptionsAsync(_context.Locations.Where(l => l.IsActive), l => l.Id, l => l.Name),
                ModuleOptions = await OptionsAsync(_context.Modules.Where(m => m.IsActive), m => m.Id, m => m.Name),
                StatusOptions = await OptionsAsync(_context.Statuses.Where(s => s.IsActive).OrderBy(s => s.SortOrder), s => s.Id, s => s.Name),
                PriorityOptions = await OptionsAsync(_context.Priorities.Where(p => p.IsActive).OrderByDescending(p => p.Level), p => p.Id, p => p.Name)
            };

            return model;
        }

        // ------------------------------------------------------------------
        // Engineer Performance (Section 27)
        // ------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> EngineerPerformance(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var effectiveDateTo = dateTo?.Date ?? DateTime.Today;
            var effectiveDateFrom = dateFrom?.Date ?? effectiveDateTo.AddDays(-29);

            var query = _context.CallLogs.AsNoTracking()
                .Where(c => c.ReportedDateTime >= effectiveDateFrom && c.ReportedDateTime < effectiveDateTo.AddDays(1));

            if (locationId.HasValue) query = query.Where(c => c.LocationId == locationId);

            var raw = await query
                .GroupBy(c => c.AttendedBy!.ApplicationUser!.FullName)
                .Select(g => new
                {
                    Engineer = g.Key,
                    Total = g.Count(),
                    Open = g.Count(c => c.Status!.Code == CallStatusCodes.Open || c.Status.Code == CallStatusCodes.Assigned),
                    InProgress = g.Count(c => c.Status!.Code == CallStatusCodes.InProgress || c.Status.Code == CallStatusCodes.OnHold),
                    Resolved = g.Count(c => c.Status!.Code == CallStatusCodes.Resolved),
                    Closed = g.Count(c => c.Status!.Code == CallStatusCodes.Closed),
                    Reopened = g.Count(c => c.Status!.Code == CallStatusCodes.Reopened),
                    AvgMinutes = g.Average(c => (double?)c.TimeTakenMinutes),
                    Critical = g.Count(c => c.Priority!.Code == PriorityCodes.Critical),
                    LineLoss = g.Count(c => c.IsLineLoss)
                })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            // SLA breach per engineer needs the same in-memory evaluation as the Dashboard
            // (comparing against each call's priority-specific resolution target).
            var slaLookup = await _context.SLAConfigurations.AsNoTracking().Where(s => s.IsActive)
                .ToDictionaryAsync(s => s.PriorityId, s => s.ResolutionMinutes);

            var slaRows = await query
                .Select(c => new { Engineer = c.AttendedBy!.ApplicationUser!.FullName, c.PriorityId, StatusCode = c.Status!.Code, c.ReportedDateTime, c.TimeTakenMinutes })
                .ToListAsync();

            var now = DateTime.Now;
            var breachByEngineer = slaRows
                .Where(c => slaLookup.ContainsKey(c.PriorityId) && (c.StatusCode == CallStatusCodes.Closed
                    ? c.TimeTakenMinutes.HasValue && c.TimeTakenMinutes.Value > slaLookup[c.PriorityId]
                    : (now - c.ReportedDateTime).TotalMinutes > slaLookup[c.PriorityId]))
                .GroupBy(c => c.Engineer)
                .ToDictionary(g => g.Key, g => g.Count());

            var rows = raw.Select(g => new EngineerPerformanceRowVm
            {
                EngineerName = g.Engineer,
                TotalCalls = g.Total,
                OpenCount = g.Open,
                InProgressCount = g.InProgress,
                ResolvedCount = g.Resolved,
                ClosedCount = g.Closed,
                ReopenedCount = g.Reopened,
                AverageResolutionMinutes = g.AvgMinutes,
                CriticalCount = g.Critical,
                LineLossCount = g.LineLoss,
                SlaBreachCount = breachByEngineer.TryGetValue(g.Engineer, out var b) ? b : 0
            }).ToList();

            var model = new EngineerPerformanceViewModel
            {
                Rows = rows,
                DateFrom = effectiveDateFrom,
                DateTo = effectiveDateTo,
                LocationId = locationId,
                LocationOptions = await OptionsAsync(_context.Locations.Where(l => l.IsActive), l => l.Id, l => l.Name)
            };

            return View(model);
        }

        // ------------------------------------------------------------------
        // SLA Report (Section 28)
        // ------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> SlaReport(DateTime? dateFrom, DateTime? dateTo, int? priorityId, int? locationId, bool breachedOnly = false)
        {
            var effectiveDateTo = dateTo?.Date ?? DateTime.Today;
            var effectiveDateFrom = dateFrom?.Date ?? effectiveDateTo.AddDays(-29);

            var query = _context.CallLogs.AsNoTracking()
                .Include(c => c.Priority)
                .Include(c => c.Status)
                .Where(c => c.ReportedDateTime >= effectiveDateFrom && c.ReportedDateTime < effectiveDateTo.AddDays(1));

            if (priorityId.HasValue) query = query.Where(c => c.PriorityId == priorityId);
            if (locationId.HasValue) query = query.Where(c => c.LocationId == locationId);

            var calls = await query
                .Select(c => new
                {
                    c.Id, c.CallNumber, PriorityName = c.Priority!.Name, c.Priority.ColorCode, c.PriorityId,
                    StatusName = c.Status!.Name, StatusCode = c.Status.Code, c.ReportedDateTime, c.TimeTakenMinutes
                })
                .ToListAsync();

            var slaLookup = await _context.SLAConfigurations.AsNoTracking().Where(s => s.IsActive)
                .ToDictionaryAsync(s => s.PriorityId, s => s.ResolutionMinutes);

            var now = DateTime.Now;
            var details = calls.Select(c =>
            {
                var hasTarget = slaLookup.TryGetValue(c.PriorityId, out var target);
                var isClosed = c.StatusCode == CallStatusCodes.Closed;
                var actualMinutes = isClosed ? (c.TimeTakenMinutes ?? 0) : (int)(now - c.ReportedDateTime).TotalMinutes;

                string status;
                if (!hasTarget) status = "Not Configured";
                else if (isClosed) status = actualMinutes > target ? "Breached" : "Met";
                else status = actualMinutes > target ? "Breached" : "Pending";

                return new SlaDetailRowVm
                {
                    CallLogId = c.Id,
                    CallNumber = c.CallNumber,
                    PriorityName = c.PriorityName,
                    PriorityColor = c.ColorCode,
                    StatusName = c.StatusName,
                    ReportedDateTime = c.ReportedDateTime,
                    ResolutionTargetMinutes = hasTarget ? target : null,
                    ActualMinutes = actualMinutes,
                    SlaStatus = status
                };
            }).ToList();

            if (breachedOnly)
            {
                details = details.Where(d => d.SlaStatus == "Breached").ToList();
            }

            var summary = details
                .GroupBy(d => new { d.PriorityName, d.PriorityColor })
                .Select(g => new SlaSummaryRowVm
                {
                    PriorityName = g.Key.PriorityName,
                    PriorityColor = g.Key.PriorityColor,
                    Total = g.Count(),
                    Met = g.Count(d => d.SlaStatus == "Met"),
                    Breached = g.Count(d => d.SlaStatus == "Breached"),
                    Pending = g.Count(d => d.SlaStatus == "Pending")
                })
                .OrderByDescending(s => s.Total)
                .ToList();

            var model = new SlaReportViewModel
            {
                Summary = summary,
                Details = details.OrderByDescending(d => d.ReportedDateTime).ToList(),
                DateFrom = effectiveDateFrom,
                DateTo = effectiveDateTo,
                PriorityId = priorityId,
                LocationId = locationId,
                BreachedOnly = breachedOnly,
                LocationOptions = await OptionsAsync(_context.Locations.Where(l => l.IsActive), l => l.Id, l => l.Name),
                PriorityOptions = await OptionsAsync(_context.Priorities.Where(p => p.IsActive).OrderByDescending(p => p.Level), p => p.Id, p => p.Name)
            };

            return View(model);
        }

        // ------------------------------------------------------------------
        // Line Loss Report (Section 24/34)
        // ------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> LineLossReport(DateTime? dateFrom, DateTime? dateTo, int? locationId, int? shopId)
        {
            var effectiveDateTo = dateTo?.Date ?? DateTime.Today;
            var effectiveDateFrom = dateFrom?.Date ?? effectiveDateTo.AddDays(-29);

            var query = _context.CallLogs.AsNoTracking()
                .Where(c => c.IsLineLoss && c.ReportedDateTime >= effectiveDateFrom && c.ReportedDateTime < effectiveDateTo.AddDays(1));

            if (locationId.HasValue) query = query.Where(c => c.LocationId == locationId);
            if (shopId.HasValue) query = query.Where(c => c.ShopId == shopId);

            var rows = await query
                .OrderByDescending(c => c.ReportedDateTime)
                .Select(c => new LineLossRowVm
                {
                    CallLogId = c.Id,
                    CallNumber = c.CallNumber,
                    LocationName = c.Location!.Name,
                    ShopName = c.Shop!.Name,
                    LineLossArea = c.LineLossArea,
                    Start = c.LineLossStartDateTime,
                    End = c.LineLossEndDateTime,
                    DurationMinutes = c.LineLossDurationMinutes,
                    AffectedVehicles = c.LineLossAffectedVehicles,
                    StatusName = c.Status!.Name
                })
                .ToListAsync();

            var byShop = await query
                .GroupBy(c => c.Shop!.Name)
                .Select(g => new LineLossByShopVm
                {
                    ShopName = g.Key,
                    IncidentCount = g.Count(),
                    TotalDowntimeMinutes = g.Sum(c => c.LineLossDurationMinutes ?? 0),
                    TotalAffectedVehicles = g.Sum(c => c.LineLossAffectedVehicles ?? 0)
                })
                .OrderByDescending(g => g.TotalDowntimeMinutes)
                .ToListAsync();

            var model = new LineLossReportViewModel
            {
                Rows = rows,
                ByShop = byShop,
                TotalIncidents = rows.Count,
                TotalDowntimeMinutes = rows.Sum(r => r.DurationMinutes ?? 0),
                TotalAffectedVehicles = rows.Sum(r => r.AffectedVehicles ?? 0),
                DateFrom = effectiveDateFrom,
                DateTo = effectiveDateTo,
                LocationId = locationId,
                ShopId = shopId,
                LocationOptions = await OptionsAsync(_context.Locations.Where(l => l.IsActive), l => l.Id, l => l.Name),
                ShopOptions = await OptionsAsync(_context.Shops.Where(s => s.IsActive), s => s.Id, s => s.Name)
            };

            return View(model);
        }

        private static async Task<List<ReportSummaryRowVm>> GroupedSummaryAsync(IQueryable<CallLog> query, System.Linq.Expressions.Expression<Func<CallLog, string>> keySelector)
        {
            var raw = await query
                .GroupBy(keySelector)
                .Select(g => new
                {
                    Label = g.Key,
                    Total = g.Count(),
                    Open = g.Count(c => c.Status!.Code != CallStatusCodes.Closed),
                    Closed = g.Count(c => c.Status!.Code == CallStatusCodes.Closed),
                    LineLoss = g.Count(c => c.IsLineLoss),
                    AvgMinutes = g.Average(c => (double?)c.TimeTakenMinutes)
                })
                .OrderByDescending(g => g.Total)
                .ToListAsync();

            return raw.Select(g => new ReportSummaryRowVm
            {
                GroupLabel = g.Label,
                Total = g.Total,
                OpenCount = g.Open,
                ClosedCount = g.Closed,
                LineLossCount = g.LineLoss,
                AverageResolutionMinutes = g.AvgMinutes
            }).ToList();
        }

        private static async Task<List<SelectListItem>> OptionsAsync<T>(IQueryable<T> source, Func<T, int> idSelector, Func<T, string> textSelector)
        {
            var items = await source.ToListAsync();
            return items.Select(i => new SelectListItem { Value = idSelector(i).ToString(), Text = textSelector(i) }).ToList();
        }
    }
}
