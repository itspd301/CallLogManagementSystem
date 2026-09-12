using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.ExtendedProperties;
using Mahindra_AD.Controllers.BaseManagement;
using Mahindra_AD.Helper;
using Mahindra_AD.Models.DbContexts;
using Mahindra_AD.Models.DTO;
using Mahindra_AD.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NPOI.SS.Formula.Functions;
using System.Data;
using System.Globalization;
using static Mahindra_AD.Controllers.OrderManagement.Build_SheetController;

namespace Mahindra_AD.Controllers
{
    public class ProductionMeetingController : BaseController
    {
        private readonly DRONAADNSKDbContext db;
        private GlobalData globalData;
        private readonly IConfiguration _configuration;
        private readonly ISessionService _sessionService;
        private readonly IUtility utility;
        private readonly FDSession fDSession;
        private readonly IGeneralService _generalService;

        public ProductionMeetingController(DRONAADNSKDbContext dbContext, IUtility _utility, IConfiguration configuration, ISessionService sessionService, CultureHelper cultureHelper, IGeneralService generalService) : base(sessionService, cultureHelper, dbContext)
        {
            db = dbContext;
            utility = _utility;
            _sessionService = sessionService;
            fDSession = sessionService.GetSession();
            globalData = new GlobalData();
            _configuration = configuration;
            _generalService = generalService;
        }

        public IActionResult Index()
        {
            globalData.pageTitle = "Production Meeting";
            globalData.controllerName = "ProductionMeetingController";
            globalData.actionName = "Index";


            ViewBag.GlobalDataModel = globalData;

            return View();
        }

        [HttpGet]
        public IActionResult GetKPIData()
        {

            var ds = utility.GetDataSet_SQL("usp_GetKPIData", CommandType.StoredProcedure, null, null, nameof(ProductionMeetingController), nameof(GetKPIData));


            if (ds == null || ds.Tables.Count == 0)
            {
                return Ok(new List<object>());
            }


            var kpiList = ds.Tables[0].AsEnumerable().Select(row => new
            {
                KPI_ID = row["KPI_ID"].ToString(),
                IndicatorID = row["IndicatorID"].ToString(),
                IndicatorName = row["IndicatorName"].ToString(),
                Description = row["Description"].ToString(),
                UnitName = row["UnitName"].ToString()
            }).ToList();

            return Ok(kpiList);
        }

        [HttpPost]
        public IActionResult SaveKPIData([FromBody] List<KPIEntryModel> data)
        {
            try
            {
                string conString = _configuration.GetConnectionString("DRONAADNSK");

                if (data == null || data.Count == 0)
                {
                    return Ok(new { result = false, message = "No Data Found" });
                }

                using (SqlConnection con = new SqlConnection(conString))
                {
                    con.Open();

                    foreach (var item in data)
                    {
                        string kpiDescription = "";
                        DateTime entryDate = item.SelectedDate ?? DateTime.Today;
                        SqlCommand descCmd = new SqlCommand(@" SELECT Description    FROM MM_PM_KPI_Master    WHERE KPI_ID = @KPI_ID", con);

                        descCmd.Parameters.AddWithValue("@KPI_ID", item.KPI_ID);

                        var descObj = descCmd.ExecuteScalar();

                        if (descObj != null)
                        {
                            kpiDescription = descObj.ToString();
                        }

                        // Check existing data
                        SqlCommand checkCmd = new SqlCommand(@"
                                SELECT
                                    Row_ID,
                                    KPI_ID,
                                    F26_Value,
                                    F27_Value,
                                    WeekValue,
                                    MonthCumValue,
                                    YTDValue,
                                    Remarks
                                FROM MM_KPI_Transaction
                                WHERE KPI_ID = @KPI_ID
                                AND CAST(EntryDate AS DATE) = @EntryDate
                            ", con);

                        checkCmd.Parameters.AddWithValue("@KPI_ID", item.KPI_ID);
                        checkCmd.Parameters.AddWithValue("@EntryDate", entryDate.Date);
                        DataTable dt = new DataTable();

                        using (SqlDataAdapter da = new SqlDataAdapter(checkCmd))
                        {
                            da.Fill(dt);
                        }

                        bool recordExists = dt.Rows.Count > 0;

                        if (recordExists)
                        {
                            DataRow dr = dt.Rows[0];
                            int transactionRowId = dr["Row_ID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Row_ID"]);
                            string oldF26 = dr["F26_Value"] == DBNull.Value ? null : dr["F26_Value"].ToString();
                            string oldF27 = dr["F27_Value"] == DBNull.Value ? null : dr["F27_Value"].ToString();
                            string oldWeek = dr["WeekValue"] == DBNull.Value ? null : dr["WeekValue"].ToString();
                            string oldMonth = dr["MonthCumValue"] == DBNull.Value ? null : dr["MonthCumValue"].ToString();
                            string oldYTD = dr["YTDValue"] == DBNull.Value ? null : dr["YTDValue"].ToString();
                            string oldRemarks = dr["Remarks"] == DBNull.Value ? null : dr["Remarks"].ToString();

                            // Audit Log
                            if ((oldF26 ?? "") != (item.F26_Value?.ToString() ?? ""))
                                InsertAuditLog(con, item.KPI_ID, transactionRowId, kpiDescription, "F26_Value", oldF26, item.F26_Value?.ToString(), "UPDATE", Convert.ToDecimal(fDSession.userId),entryDate);

                            if ((oldF27 ?? "") != (item.F27_Value?.ToString() ?? ""))
                                InsertAuditLog(con, item.KPI_ID, transactionRowId, kpiDescription, "F27_Value", oldF27, item.F27_Value?.ToString(), "UPDATE", Convert.ToDecimal(fDSession.userId), entryDate);

                            if ((oldWeek ?? "") != (item.WeekValue?.ToString() ?? ""))
                                InsertAuditLog(con, item.KPI_ID, transactionRowId, kpiDescription, "WeekValue", oldWeek, item.WeekValue?.ToString(), "UPDATE", Convert.ToDecimal(fDSession.userId), entryDate);

                            if ((oldMonth ?? "") != (item.MonthCumValue?.ToString() ?? ""))
                                InsertAuditLog(con, item.KPI_ID, transactionRowId, kpiDescription, "MonthCumValue", oldMonth, item.MonthCumValue?.ToString(), "UPDATE", Convert.ToDecimal(fDSession.userId), entryDate);

                            if ((oldYTD ?? "") != (item.YTDValue?.ToString() ?? ""))
                                InsertAuditLog(con, item.KPI_ID, transactionRowId, kpiDescription, "YTDValue", oldYTD, item.YTDValue?.ToString(), "UPDATE", Convert.ToDecimal(fDSession.userId), entryDate);

                            if ((oldRemarks ?? "") != (item.Remarks ?? ""))
                                InsertAuditLog(con, item.KPI_ID, transactionRowId, kpiDescription, "Remarks", oldRemarks, item.Remarks, "UPDATE", Convert.ToDecimal(fDSession.userId), entryDate);

                            // Update
                            SqlCommand updateCmd = new SqlCommand(@"
                                UPDATE MM_KPI_Transaction
                                SET
                                    F26_Value = @F26,
                                    F27_Value = @F27,
                                    WeekValue = @Week,
                                    MonthCumValue = @Month,
                                    YTDValue = @YTD,
                                    Remarks = @Remarks,
                                    Updated_Date = GETDATE(),
                                    Updated_User_ID = @UserID
                                WHERE KPI_ID = @KPI_ID
                                AND CAST(EntryDate AS DATE) = @EntryDate
                            ", con);

                            updateCmd.Parameters.AddWithValue("@KPI_ID", item.KPI_ID);
                            updateCmd.Parameters.AddWithValue("@UserID", fDSession.userId);
                            updateCmd.Parameters.AddWithValue("@F26", (object)item.F26_Value ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@F27", (object)item.F27_Value ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@Week", (object)item.WeekValue ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@Month", (object)item.MonthCumValue ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@YTD", (object)item.YTDValue ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@Remarks", (object)item.Remarks ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@EntryDate", entryDate.Date);

                            updateCmd.ExecuteNonQuery();
                        }
                        else
                        {
                            // Insert
                            SqlCommand insertCmd = new SqlCommand(@"
                        INSERT INTO MM_KPI_Transaction
                        (
                            EntryDate,
                            WeekNo,
                            MonthNo,
                            YearNo,
                            KPI_ID,
                            ModelID,
                            F26_Value,
                            F27_Value,
                            WeekValue,
                            MonthCumValue,
                            YTDValue,
                            Remarks,
                            Inserted_Date,
                            Inserted_User_ID
                        )
                        VALUES
                        (
                           @EntryDate,
                            @WeekNo,
                            @MonthNo,
                            @YearNo,
                            @KPI_ID,
                            @ModelID,
                            @F26,
                            @F27,
                            @Week,
                            @Month,
                            @YTD,
                            @Remarks,
                            GETDATE(),
                            @UserID
                        )
                    ", con);

                            insertCmd.Parameters.AddWithValue("@KPI_ID", item.KPI_ID);
                            insertCmd.Parameters.AddWithValue("@ModelID", item.ModelID);
                            insertCmd.Parameters.AddWithValue("@UserID", fDSession.userId);
                            insertCmd.Parameters.AddWithValue("@F26", (object)item.F26_Value ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@F27", (object)item.F27_Value ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@Week", (object)item.WeekValue ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@Month", (object)item.MonthCumValue ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@YTD", (object)item.YTDValue ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@Remarks", (object)item.Remarks ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@EntryDate", entryDate.Date);
                            insertCmd.Parameters.AddWithValue("@WeekNo", ISOWeek.GetWeekOfYear(entryDate));
                            insertCmd.Parameters.AddWithValue("@MonthNo", entryDate.Month);
                            insertCmd.Parameters.AddWithValue("@YearNo", entryDate.Year);


                            insertCmd.ExecuteNonQuery();

                            // Audit Logs
                            InsertAuditLog(con, item.KPI_ID, 0, kpiDescription, "F26_Value", null, item.F26_Value?.ToString(), "INSERT", Convert.ToDecimal(fDSession.userId), entryDate);

                            InsertAuditLog(con, item.KPI_ID, 0, kpiDescription, "F27_Value", null, item.F27_Value?.ToString(), "INSERT", Convert.ToDecimal(fDSession.userId), entryDate);

                            InsertAuditLog(con, item.KPI_ID, 0, kpiDescription, "WeekValue", null, item.WeekValue?.ToString(), "INSERT", Convert.ToDecimal(fDSession.userId), entryDate);

                            InsertAuditLog(con, item.KPI_ID, 0, kpiDescription, "MonthCumValue", null, item.MonthCumValue?.ToString(), "INSERT", Convert.ToDecimal(fDSession.userId), entryDate);

                            InsertAuditLog(con, item.KPI_ID, 0, kpiDescription, "YTDValue", null, item.YTDValue?.ToString(), "INSERT", Convert.ToDecimal(fDSession.userId), entryDate);

                            InsertAuditLog(con, item.KPI_ID, 0, kpiDescription, "Remarks", null, item.Remarks, "INSERT", Convert.ToDecimal(fDSession.userId), entryDate);
                        }
                    }
                }

                return Ok(new { result = true, message = "Saved Successfully" });
            }
            catch (Exception e)
            {
                _generalService.addControllerException(e, nameof(ProductionMeetingController), nameof(SaveKPIData), Convert.ToInt32(fDSession.userId));

                return Ok(new
                {
                    result = false,
                    message = "Not Saved " + e.Message
                });
            }
        }

        private void InsertAuditLog(SqlConnection con, int kpiId, int transactionRowId, string kpiDescription, string columnName, string oldValue, string newValue, string actionType, decimal userId,DateTime entryDate)
        {

            try
            {

                SqlCommand logCmd = new SqlCommand(@"
                    INSERT INTO MM_KPI_Transaction_Log
                    (
                        KPI_ID, 
                        Transaction_Row_ID,
                        KPI_Description,
                        Column_Name,
                        Old_Value,
                        New_Value,
                        Action_Type,
                        Changed_By,
                        Changed_Date,
                        EntryDate
                    )
                    VALUES
                    (
                        @KPI_ID,
                        @Transaction_Row_ID,
                        @KPI_Description,
                        @Column_Name,
                        @Old_Value,
                        @New_Value,
                        @Action_Type,
                        @Changed_By,
                        GETDATE(),
                        @EntryDate
                    )", con);

        logCmd.Parameters.AddWithValue("@KPI_ID", kpiId);
                logCmd.Parameters.AddWithValue("@Transaction_Row_ID", transactionRowId);
                logCmd.Parameters.AddWithValue("@KPI_Description", (object) kpiDescription ?? DBNull.Value);
                logCmd.Parameters.AddWithValue("@Column_Name", columnName);
                logCmd.Parameters.AddWithValue("@Old_Value", (object) oldValue ?? DBNull.Value);
                logCmd.Parameters.AddWithValue("@New_Value", (object) newValue ?? DBNull.Value);
                logCmd.Parameters.AddWithValue("@Action_Type", actionType);
                logCmd.Parameters.AddWithValue("@Changed_By", userId);
                logCmd.Parameters.AddWithValue("@EntryDate", entryDate.Date);
                logCmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                _generalService.addControllerException(e, nameof(ProductionMeetingController), nameof(InsertAuditLog), Convert.ToInt32(fDSession.userId));

            }


        }

        [HttpGet]
public IActionResult GetKPITransactionData(DateTime? selectedDate)
{
    DateTime date = selectedDate ?? DateTime.Today;

    var query = " SELECT KPI_ID, ModelID, F26_Value, F27_Value, WeekValue, MonthCumValue, YTDValue, Remarks FROM MM_KPI_Transaction WHERE CAST(EntryDate AS DATE) = @SelectedDate ";
    SqlParameter[] param =
    {
                new SqlParameter("@SelectedDate", date.Date)
            };

    var ds = utility.GetDataSet_SQL(query, CommandType.Text, param, null, nameof(ProductionMeetingController), nameof(GetKPITransactionData));


    if (ds == null || ds.Tables.Count == 0)
    {
        return Ok(new List<KPIEntryModel>());
    }

    var list = ds.Tables[0].AsEnumerable().Select(row => new KPIEntryModel
    {
        KPI_ID = Convert.ToInt32(row["KPI_ID"]),
        ModelID = Convert.ToInt32(row["ModelID"]),
        F26_Value = row["F26_Value"] == DBNull.Value ? null : (decimal?)row["F26_Value"],
        F27_Value = row["F27_Value"] == DBNull.Value ? null : (decimal?)row["F27_Value"],
        WeekValue = row["WeekValue"] == DBNull.Value ? null : (decimal?)row["WeekValue"],
        MonthCumValue = row["MonthCumValue"] == DBNull.Value ? null : (decimal?)row["MonthCumValue"],
        YTDValue = row["YTDValue"] == DBNull.Value ? null : (decimal?)row["YTDValue"],
        Remarks = row["Remarks"]?.ToString()
    }).ToList();

    return Ok(list);
}

public class KPIEntryModel
{
    public int KPI_ID { get; set; }
    public int ModelID { get; set; }

    public DateTime? SelectedDate { get; set; }

    public decimal? F26_Value { get; set; }
    public decimal? F27_Value { get; set; }

    public decimal? WeekValue { get; set; }
    public decimal? MonthCumValue { get; set; }
    public decimal? YTDValue { get; set; }

    public string Remarks { get; set; }
}

public IActionResult KPI_Master_Index()
{
    globalData.pageTitle = "KPI Master Index";
    globalData.controllerName = "ProductionMeetingController";
    globalData.actionName = "KPI_Master_Index";


    ViewBag.GlobalDataModel = globalData;
    return View();
}
[HttpPost]
public IActionResult GetKPIMasterData()
{
    var draw = Request.Form["draw"].FirstOrDefault();
    var start = Request.Form["start"].FirstOrDefault();
    var length = Request.Form["length"].FirstOrDefault();
    var searchValue = Request.Form["search[value]"].FirstOrDefault();
    var orderColumn = Request.Form["order[0][column]"].FirstOrDefault();
    var orderDir = Request.Form["order[0][dir]"].FirstOrDefault();

    int skip = string.IsNullOrEmpty(start) ? 0 : Convert.ToInt32(start);
    int pageSize = string.IsNullOrEmpty(length) ? 10 : Convert.ToInt32(length);

    // 1. Get Total Count first
    string countQuery = @"
                        SELECT COUNT(*) 
                        FROM MM_PM_KPI_Master mpkm
                        JOIN MM_PM_Indicator_Master mpim ON mpkm.Indicator_ID = mpim.Row_ID
                        JOIN MM_PM_Unit_Master mpum ON mpkm.Unit_ID = mpum.UnitID";

    var dsTotal = utility.GetDataSet_SQL(countQuery, CommandType.Text, null, null, nameof(ProductionMeetingController), nameof(GetKPIMasterData));
    int recordsTotal = 0;
    if (dsTotal != null && dsTotal.Tables.Count > 0 && dsTotal.Tables[0].Rows.Count > 0)
    {
        recordsTotal = Convert.ToInt32(dsTotal.Tables[0].Rows[0][0]);
    }

    // 2. Build the Search Filter Condition
    string searchCondition = "";
    if (!string.IsNullOrWhiteSpace(searchValue))
    {
        // Safe string escape for inline query execution
        string cleanSearch = searchValue.Replace("'", "''");
        searchCondition = $@" AND (
                    mpim.IndicatorName LIKE '%{cleanSearch}%' OR 
                    mpum.UnitName LIKE '%{cleanSearch}%' OR 
                    mpkm.Description LIKE '%{cleanSearch}%'
                )";
    }

    // 3. Get Filtered Count
    string filteredCountQuery = countQuery + " WHERE 1=1 " + searchCondition;
    var dsFiltered = utility.GetDataSet_SQL(filteredCountQuery, CommandType.Text, null, null, nameof(ProductionMeetingController), nameof(GetKPIMasterData));
    int recordsFiltered = 0;
    if (dsFiltered != null && dsFiltered.Tables.Count > 0 && dsFiltered.Tables[0].Rows.Count > 0)
    {
        recordsFiltered = Convert.ToInt32(dsFiltered.Tables[0].Rows[0][0]);
    }

    // 4. Handle Sorting Column Name mapping
    string orderByColumn = "mpkm.KPI_ID";
    string direction = (orderDir == "desc") ? "DESC" : "ASC";

    switch (orderColumn)
    {
        case "0": orderByColumn = "mpkm.KPI_ID"; break;
        case "1": orderByColumn = "mpim.IndicatorID"; break;
        case "2": orderByColumn = "mpim.IndicatorName"; break;
        case "3": orderByColumn = "mpum.UnitName"; break;
        default: orderByColumn = "mpkm.KPI_ID DESC"; direction = ""; break;
    }

    string sortClause = string.IsNullOrEmpty(direction) ? orderByColumn : $"{orderByColumn} {direction}";

    // 5. Final Query with Pagination (OFFSET / FETCH NEXT)
    string finalQuery = $@"
                    SELECT mpkm.KPI_ID, mpim.IndicatorID, mpim.IndicatorName, mpum.UnitName, mpkm.Description   
                    FROM MM_PM_KPI_Master mpkm
                    JOIN MM_PM_Indicator_Master mpim ON mpkm.Indicator_ID = mpim.Row_ID
                    JOIN MM_PM_Unit_Master mpum ON mpkm.Unit_ID = mpum.UnitID
                    WHERE 1=1 {searchCondition}
                    ORDER BY {sortClause}
                    OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY";

    var dsData = utility.GetDataSet_SQL(finalQuery, CommandType.Text, null, null, nameof(ProductionMeetingController), nameof(GetKPIMasterData));
    var dataList = new List<object>();

    if (dsData != null && dsData.Tables.Count > 0)
    {
        dataList = dsData.Tables[0].AsEnumerable().Select(row => new
        {
            KPI_ID = Convert.ToInt32(row["KPI_ID"]),
            IndicatorID = row["IndicatorID"].ToString(),
            IndicatorName = row["IndicatorName"].ToString(),
            UnitName = row["UnitName"].ToString(),
            Description = row["Description"].ToString()
        }).ToList<object>();
    }

    // 6. Return DataTables compatible JSON
    return Json(new
    {
        draw = draw,
        recordsTotal = recordsTotal,
        recordsFiltered = recordsFiltered,
        data = dataList
    });
}
public IActionResult KPI_Master_Create()
{
    globalData.pageTitle = "KPI Master Create";
    globalData.controllerName = "ProductionMeetingController";
    globalData.actionName = "KPI_Master_Create";


    ViewBag.GlobalDataModel = globalData;
    return View();
}
[HttpGet]
public JsonResult GetIndicators()
{

    string query = "SELECT Row_ID, IndicatorID, IndicatorName FROM MM_PM_Indicator_Master";

    var ds = utility.GetDataSet_SQL(query, CommandType.Text, null, null, nameof(ProductionMeetingController), nameof(GetIndicators));

    var dataList = new List<object>();

    if (ds != null && ds.Tables.Count > 0)
    {
        dataList = ds.Tables[0].AsEnumerable().Select(row => new
        {
            Row_ID = Convert.ToInt32(row["Row_ID"]),
            IndicatorID = row["IndicatorID"].ToString(),
            IndicatorName = row["IndicatorName"].ToString()
        }).ToList<object>();
    }

    return Json(dataList);
}

[HttpGet]
public JsonResult GetUnits()
{
    string query = "SELECT UnitID, UnitName FROM MM_PM_Unit_Master";

    var ds = utility.GetDataSet_SQL(query, CommandType.Text, null, null, nameof(ProductionMeetingController), nameof(GetUnits));

    var dataList = new List<object>();

    if (ds != null && ds.Tables.Count > 0)
    {
        dataList = ds.Tables[0].AsEnumerable().Select(row => new
        {
            UnitID = Convert.ToInt32(row["UnitID"]),
            UnitName = row["UnitName"].ToString()
        }).ToList<object>();
    }

    return Json(dataList);
}
public class KPI_Master
{
    public int KPI_ID { get; set; }
    public int Indicator_ID { get; set; }
    public int Unit_ID { get; set; }
    public string Description { get; set; }
    public int? DisplayOrder { get; set; }
}
[HttpPost]
public IActionResult CreateKPI([FromBody] KPI_Master model)
{
    try
    {

        if (model == null)
            return BadRequest();

        string query = @"
                INSERT INTO MM_PM_KPI_Master (Indicator_ID, Unit_ID, Description, DisplayOrder)
                VALUES (@Indicator_ID, @Unit_ID, @Description, @DisplayOrder)";

        SqlParameter[] param = new SqlParameter[]
        {
                new SqlParameter("@Indicator_ID", model.Indicator_ID == 0 ? (object)DBNull.Value : model.Indicator_ID),
                new SqlParameter("@Unit_ID", model.Unit_ID == 0 ? (object)DBNull.Value : model.Unit_ID),
                new SqlParameter("@Description", string.IsNullOrEmpty(model.Description) ? (object)DBNull.Value : model.Description),
                new SqlParameter("@DisplayOrder", (object)model.DisplayOrder ?? DBNull.Value)
        };

        int i = utility.ExecuteNonQuery_SQL(query, CommandType.Text, param, null, nameof(ProductionMeetingController), nameof(CreateKPI));

        return Json(new { success = true, msg = "Saved Successfully" });

    }
    catch (Exception e)
    {
        return Json(new { success = false, msg = e.Message });

    }
}
[HttpGet]
public IActionResult KPI_Master_Edit(int id)
{
    globalData.pageTitle = "KPI Master Edit";
    globalData.controllerName = "ProductionMeetingController";
    globalData.actionName = "KPI_Master_Edit";


    ViewBag.GlobalDataModel = globalData;

    string query = @"  SELECT Indicator_ID, Unit_ID, Description, DisplayOrder FROM MM_PM_KPI_Master WHERE KPI_ID = @KPI_ID";

    SqlParameter[] param = new SqlParameter[]
    {
                new SqlParameter("@KPI_ID", id)
    };

    var ds = utility.GetDataSet_SQL(query, CommandType.Text, param, null, nameof(ProductionMeetingController), nameof(KPI_Master_Edit));

    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
        return RedirectToAction("KPI_Master_Index");

    var row = ds.Tables[0].Rows[0];

    var model = new
    {
        KPI_ID = id,
        Indicator_ID = Convert.ToInt32(row["Indicator_ID"]),
        Unit_ID = Convert.ToInt32(row["Unit_ID"]),
        Description = row["Description"].ToString(),
        DisplayOrder = row["DisplayOrder"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["DisplayOrder"])
    };

    return View(model);
}
[HttpPost]
public IActionResult UpdateKPI([FromBody] KPI_Master model)
{
    try
    {
        string query = @"
                        UPDATE MM_PM_KPI_Master
                        SET Indicator_ID = @Indicator_ID,
                            Unit_ID = @Unit_ID,
                            Description = @Description,
                            DisplayOrder = @DisplayOrder
                        WHERE KPI_ID = @KPI_ID";

        SqlParameter[] param = new SqlParameter[]
        {
                    new SqlParameter("@KPI_ID", model.KPI_ID),
                    new SqlParameter("@Indicator_ID", model.Indicator_ID),
                    new SqlParameter("@Unit_ID", model.Unit_ID),
                    new SqlParameter("@Description", model.Description ?? (object)DBNull.Value),
                    new SqlParameter("@DisplayOrder", (object)model.DisplayOrder ?? DBNull.Value)
        };

        int i = utility.ExecuteNonQuery_SQL(query, CommandType.Text, param, null, nameof(ProductionMeetingController), nameof(UpdateKPI));

        return Json(new { success = true, msg = "Updated Successfully" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, msg = ex.Message });
    }
}

[HttpPost]
public IActionResult DeleteKPI(int id)
{
    try
    {
        string query = "DELETE FROM MM_PM_KPI_Master WHERE KPI_ID = @KPI_ID";

        SqlParameter[] param = new SqlParameter[]
        {
            new SqlParameter("@KPI_ID", id)
        };

        utility.ExecuteNonQuery_SQL(query, CommandType.Text, param, null, nameof(ProductionMeetingController), nameof(DeleteKPI));

        return Ok(new { result = true, message = "Deleted Successfully" });
    }
    catch (Exception ex)
    {
        return Ok(new { result = false, message = "Delete failed: " + ex.Message });
    }
}



    }
}
