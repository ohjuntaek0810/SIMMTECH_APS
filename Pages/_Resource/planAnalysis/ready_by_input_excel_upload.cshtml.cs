using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text;
using System.Text.Json;
//using System;

namespace HS.Web.Pages
{
    public class ready_by_input_excel_upload : BasePageModel
    {
        public ready_by_input_excel_upload()
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
            this.OnPostFetchHandler = postFetchHandler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
                toClient["fileName"] = this.SearchFileName(terms);
            }

            else if (e.Command == "update")
            {
                try
                {
                    this.Update();
                }
                catch (Exception ex)
                {
                    toClient["data"] = ex.StackTrace;
                }

                toClient["data"] = "success";
            }

            return toClient;
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable Search(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
    *
from
    TH_TAR_READY_BY_INPUT_L
WHERE
    1=1
");
            if (terms["group_id"].Length > 0)
            {
                if (terms["group_id"].AsString() == "SPS")
                {
                    sSQL.Append($@"
    AND PROD_GROUP = 'S'
");
                }

                if (terms["group_id"].AsString() == "HDI")
                {
                    sSQL.Append($@"
    AND PROD_GROUP = 'H'
");
                }
            }


            return Data.Get(sSQL.ToString()).Tables[0]; 
        }

        private DataTable SearchFileName(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
	TOP 1
	*
from 
	TH_GUI_UPLOAD_FILE
where
	MENU_CODE = 'PA010203'
    AND DIVISION_ID = {terms["group_id"].V}
order by INSERT_DTTM DESC
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private void Update()
        {
            // 위에 코드로 프로시저 실행시키면 Transaction 관련 에러 발생
            (string dbType, string connection) = Data.GetConnection("Default");

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("PR_READY_BY_INPUT_ORDER_NUMBER", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            if (command == "ExcelDownload")
            {
                //데이터 조회한 값으로 엑셀 다운로드
                DataTable dtResult = this.Search(e.Params["terms"]);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Lot_Routing_Sequence_" + timestamp);
            }
            else
                return Page();
        }

        private IActionResult postFetchHandler(PostArgs e)
        {
            string returnMessage = "";

            if (e.Command == "excelUplaod")
            {
                IFormFile file = e.Files[0];
                DataSet ds = HS.Core.Excel.Import(file.OpenReadStream());
                ParamList excelData = ds.Tables[0].ToParamList();


                Params terms = e.Params["terms"];

                // 엑셀 저장 시 DIVISION 체크해야함
                Vali vali = new Vali(terms);
                vali.Null("group_id", "DIVISION을 선택해주세요.");

                vali.DoneDeco();

                string division = terms["group_id"].AsString();

                DataTable dt = ds.Tables[0];

                string[] dateColumns = { "New Start", "New Date" };

                foreach (DataRow row in dt.Rows)
                {
                    foreach (DataColumn col in dt.Columns)
                    {
                        var value = row[col];

                        // 날짜 컬럼 개별 처리
                        if (dateColumns.Contains(col.ColumnName))
                        {
                            if (string.IsNullOrWhiteSpace(value?.ToString()))
                            {
                                row[col] = DBNull.Value;
                            }
                            else if (DateTime.TryParse(value.ToString(), out DateTime parsedDate))
                            {
                                row[col] = parsedDate;
                                //Console.WriteLine($"문자열 날짜 변환됨: {col.ColumnName} = {parsedDate}");
                            }
                            else if (double.TryParse(value.ToString(), out double oaDate))
                            {
                                row[col] = DateTime.FromOADate(oaDate);
                                //Console.WriteLine($"시리얼 날짜 변환됨: {col.ColumnName} = {oaDate}");
                            }
                            else
                            {
                                //Console.WriteLine($"날짜 변환 실패: {col.ColumnName} = {value}");
                                row[col] = DBNull.Value;
                            }

                            continue;
                        }

                        // 나머지 컬럼 처리
                        if (string.IsNullOrWhiteSpace(value?.ToString()))
                        {
                            row[col] = DBNull.Value;
                        }
                    }
                }

                

                // 날짜 형식 포맷 변경 필요.
                // 등록자 및 등록날자 넣어줘야함
                if (!ds.Tables[0].Columns.Contains("INSERT_ID"))
                    ds.Tables[0].Columns.Add("INSERT_ID", typeof(string));
                if (!ds.Tables[0].Columns.Contains("INSERT_DTTM"))
                    ds.Tables[0].Columns.Add("INSERT_DTTM", typeof(DateTime));
                if (!ds.Tables[0].Columns.Contains("UPDATE_ID"))
                    ds.Tables[0].Columns.Add("UPDATE_ID", typeof(string));
                if (!ds.Tables[0].Columns.Contains("UPDATE_DTTM"))
                    ds.Tables[0].Columns.Add("UPDATE_DTTM", typeof(DateTime));

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    if (row["New Start"].ToString() != "")
                    {
                        row["New Start"] = DateTime.Parse(Convert.ToDateTime(row["New Start"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    else
                    {
                        row["New Start"] = DBNull.Value;
                    }
                    if (row["New Date"].ToString() != "")
                    {
                        row["New Date"] = DateTime.Parse(Convert.ToDateTime(row["New Date"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    else
                    {
                        row["New Date"] = DBNull.Value;
                    }

                    if (division == "SPS")
                    {
                        row["Prod Group"] = "S";
                    }
                    else if (division == "HDI")
                    {
                        row["Prod Group"] = "H";
                    }

                    row["INSERT_ID"] = Cookie<User>.Store.USER_ID;
                    row["INSERT_DTTM"] = DateTime.Now;
                    row["UPDATE_ID"] = Cookie<User>.Store.USER_ID;
                    row["UPDATE_DTTM"] = DateTime.Now;
                }


                // bulk 
                (string dbType, string connection) = Data.GetConnection("Default");
                Console.WriteLine($"GetConnection: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        Console.WriteLine($"BeginTransaction: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        try
                        {
                            StringBuilder sSQL = new StringBuilder();
                            //  선택받은 DIVISION에 해당하는것만 삭제하고 다시 올려야함.
                            sSQL.Append(@"
DELETE FROM TH_TAR_READY_BY_INPUT_L WHERE 1=1
");
                            if (division == "SPS")
                            {
                                sSQL.Append($@"
AND PROD_GROUP = 'S'
");
                            }
                            else if (division == "HDI")
                            {
                                sSQL.Append($@"
AND PROD_GROUP = 'H'
");
                            }


                            using (SqlCommand deleteCmd = new SqlCommand(sSQL.ToString(), conn, tx))
                            {
                                Console.WriteLine($"deleteCmd: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                deleteCmd.ExecuteNonQuery(); // 이걸로해야 tx가 먹음
                            }

                            StringBuilder updateSQL = new StringBuilder();
                            updateSQL.Append($@"UPDATE TH_TAR_READY_BY_INPUT_H SET USE_YN = 'N' WHERE DIVISION_ID = '{division}'");

                            using (SqlCommand updateCmd = new SqlCommand(updateSQL.ToString(), conn, tx))
                            {
                                Console.WriteLine($"updateCmd: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                updateCmd.ExecuteNonQuery();
                            }




                            // 헤더에 버전 넣기
                            StringBuilder sSQL2 = new StringBuilder();

                            sSQL2.Append($@"
insert into [TH_TAR_READY_BY_INPUT_H]
select 
	'101' AS ORGANIZATION_ID,
	'{division}' AS DIVISION_ID,
	'{division}' + '_' + format(getdate(), 'yyyyMMdd') + '_' + format((select MAX(VER_SN) + 1 FROM TH_TAR_READY_BY_INPUT_H), '000') AS VERSION,
	format(getdate(), 'yyyy-MM-dd') AS CREATE_DATE,
	(select MAX(VER_SN) + 1 FROM TH_TAR_READY_BY_INPUT_H) AS VER_SN,
	'AVAILABLE' AS VERSION_STATUS,
	'Y' AS USE_YN,
	'MP_250319A' As MPS_PLAN_ID,
	null AS DESCR,
	'admin' AS INSERT_ID,
	getdate() AS INSERT_DTTM,
	'admin' AS UPDATE_ID,
	getdate() AS UPDATE_DTTM
");
                            using (SqlCommand insertCmd = new SqlCommand(sSQL2.ToString(), conn, tx))
                            {
                                Console.WriteLine($"insertCmd: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                //HS.Web.Common.Data.Execute(sSQL2.ToString());
                                insertCmd.ExecuteNonQuery();
                            }

                            // 파일정보 저장하기 위해 파일 명 체크
                            string uploadedFileName = file.FileName;

                            StringBuilder sSQL3 = new StringBuilder();
                            sSQL3.Append($@"
INSERT INTO TH_GUI_UPLOAD_FILE (
    DIVISION_ID,
	FILE_NAME,
	MENU_CODE,
	INSERT_ID,
	INSERT_DTTM
) VALUES (
    '{division}',
    '{uploadedFileName}',
    'PA010203',
    '{Cookie<User>.Store.USER_ID}',
    GETDATE()
)
");
                            // 파일정보 저장
                            using (SqlCommand cmd = new SqlCommand(sSQL3.ToString(), conn, tx))
                            {
                                Console.WriteLine($"cmd: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                cmd.ExecuteNonQuery(); // 이걸로해야 tx가 먹음
                            }



                            // INSERT 후 VERSION 가져오기
                            StringBuilder sSQL4 = new StringBuilder();
                            sSQL4.Append($@"
SELECT TOP 1 VERSION 
FROM TH_TAR_READY_BY_INPUT_H 
WHERE DIVISION_ID = '{division}' 
ORDER BY INSERT_DTTM DESC
");
                            //DataTable dt2 = Data.Get(sSQL4.ToString()).Tables[0];
                            //string version = dt2.Rows[0]["VERSION"].ToString();
                            string version = "";

                            using (SqlCommand cmd = new SqlCommand(sSQL4.ToString(), conn, tx))
                            {
                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        version = reader["VERSION"].ToString();
                                    }
                                }
                            }

                            // VERSION 컬럼이 없을 경우 넣어줘야함.
                            if (!ds.Tables[0].Columns.Contains("VERSION"))
                            {
                                Console.WriteLine($"++VERSION: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                ds.Tables[0].Columns.Add("VERSION", typeof(string));

                                foreach (DataRow row in ds.Tables[0].Rows)
                                {
                                    row["VERSION"] = version;
                                }
                                Console.WriteLine($"--VERSION: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            }




                            //HS.Web.Proc.TH_TAR_READY_BY_INPUT_L.Save(excelData, division, version);

                            using (SqlBulkCopy bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx))
                            {
                                Console.WriteLine($"bulk 시작: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                                // Timeout 시간 조정
                                bulk.BulkCopyTimeout = 300;

                                bulk.DestinationTableName = "dbo.TH_TAR_READY_BY_INPUT_L";

                                bulk.ColumnMappings.Add("VERSION", "VERSION");

                                bulk.ColumnMappings.Add("Prod Group", "PROD_GROUP");
                                bulk.ColumnMappings.Add("Release Status", "RELEASE_STATUS");
                                bulk.ColumnMappings.Add("Primary", "PRIMARY_DESC");
                                bulk.ColumnMappings.Add("Alternate", "ALTERNATE_DESC");
                                bulk.ColumnMappings.Add("BOM", "CCL_QTY");
                                bulk.ColumnMappings.Add("Fixed lot multiplier", "FLM");
                                bulk.ColumnMappings.Add("MFG Category", "MFG_CATEGORY");
                                bulk.ColumnMappings.Add("Layer", "LAYER");
                                bulk.ColumnMappings.Add("Cust Name", "CUST_NAME");
                                bulk.ColumnMappings.Add("Lay-up Type", "LAYUP_DESC");
                                bulk.ColumnMappings.Add("Shrinkage Rate", "SHRINKAGE_RATE");
                                bulk.ColumnMappings.Add("Item Code", "ITEM_CODE");
                                bulk.ColumnMappings.Add("Model Name", "MODEL_NAME");
                                bulk.ColumnMappings.Add("Lead Time", "LEAD_TIME");
                                bulk.ColumnMappings.Add("New Start", "NEW_START_DATE");
                                bulk.ColumnMappings.Add("New Date", "NEW_DATE");
                                bulk.ColumnMappings.Add("Qty Pcs", "QTY_PCS");
                                bulk.ColumnMappings.Add("Pnl Qty", "PNL_QTY");
                                bulk.ColumnMappings.Add("M2 Qty", "M2_QTY");
                                bulk.ColumnMappings.Add("Component Item Code", "COMPONENT_ITEM_CODE");
                                bulk.ColumnMappings.Add("Component Item Desc", "COMPONENT_ITEM_DESC");
                                bulk.ColumnMappings.Add("ST1001", "ST1001");
                                bulk.ColumnMappings.Add("ST1003", "ST1003");
                                bulk.ColumnMappings.Add("ST1008", "ST1008");
                                bulk.ColumnMappings.Add("ST1010", "ST1010");
                                bulk.ColumnMappings.Add("IDF_HDI", "IDF_HDI");
                                bulk.ColumnMappings.Add("MLB_HDI", "MLB_HDI");
                                bulk.ColumnMappings.Add("Drill_HDI", "DRILL_HDI");
                                bulk.ColumnMappings.Add("Laser_Drill_HDI", "LASER_DRILL_HDI");
                                bulk.ColumnMappings.Add("Cu_plating_HDI", "CU_PLATING_HDI");
                                bulk.ColumnMappings.Add("Des_Deb_HDI", "DES_DEB_HDI");
                                bulk.ColumnMappings.Add("Via_Fill_HDI", "VIA_FILL_HDI");
                                bulk.ColumnMappings.Add("ODF_HDI", "ODF_HDI");
                                bulk.ColumnMappings.Add("Hole_Plugging_HDI", "HOLE_PLUGGING_HDI");
                                bulk.ColumnMappings.Add("SM_Spray_HDI", "SM_SPRAY_HDI");
                                bulk.ColumnMappings.Add("SM_Screen_HDI", "SM_SCREEN_HDI");
                                bulk.ColumnMappings.Add("Marking_HDI", "MARKING_HDI");
                                bulk.ColumnMappings.Add("Au_masking_HDI", "AU_MASKING_HDI");
                                bulk.ColumnMappings.Add("Stripping_HDI", "STRIPPING_HDI");
                                bulk.ColumnMappings.Add("Hard_Au_HDI", "HARD_AU_HDI");
                                bulk.ColumnMappings.Add("Router_HDI", "ROUTER_HDI");
                                bulk.ColumnMappings.Add("Chamfer_HDI", "CHAMFER_HDI");
                                bulk.ColumnMappings.Add("BBT_HDI", "BBT_HDI");
                                bulk.ColumnMappings.Add("OSP_HDI", "OSP_HDI");
                                bulk.ColumnMappings.Add("AFVI_HDI", "AFVI_HDI");
                                bulk.ColumnMappings.Add("MLB", "MLB");
                                bulk.ColumnMappings.Add("Half Etching", "HALF_ETCHING");
                                bulk.ColumnMappings.Add("Laser Drill", "LASER_DRILL");
                                bulk.ColumnMappings.Add("Eless", "ELESS");
                                bulk.ColumnMappings.Add("Tenting", "TENTING");
                                bulk.ColumnMappings.Add("VIA FILL", "VIAFILL");
                                bulk.ColumnMappings.Add("HOLEPLUG", "HOLEPLUG");
                                bulk.ColumnMappings.Add("DFSR/FLAT", "DFSR_FLAT");
                                bulk.ColumnMappings.Add("DFSR", "DFSR");
                                bulk.ColumnMappings.Add("FLAT", "FLAT");
                                bulk.ColumnMappings.Add("Hard AU", "HARDAU");
                                bulk.ColumnMappings.Add("ENEPIG", "ENEPIG");
                                bulk.ColumnMappings.Add("Slot punch", "SLOTPUNCH");
                                bulk.ColumnMappings.Add("BBT", "BBT");
                                bulk.ColumnMappings.Add("SB", "SB");
                                bulk.ColumnMappings.Add("SOP", "SOP");
                                bulk.ColumnMappings.Add("Soft Au", "SOFTAU");
                                bulk.ColumnMappings.Add("SM", "SM");
                                bulk.ColumnMappings.Add("ITS", "ITS");
                                bulk.ColumnMappings.Add("LDI(I/L)", "LDI_IL");
                                bulk.ColumnMappings.Add("LDI(O/L)", "LDI_OL");
                                bulk.ColumnMappings.Add("LDI(L/M)", "LDI_LM");
                                bulk.ColumnMappings.Add("DI(SM)", "DI_SM");
                                bulk.ColumnMappings.Add("PF(I/L)", "PF_IL");
                                bulk.ColumnMappings.Add("PF(O/L)", "PF_OL");
                                bulk.ColumnMappings.Add("PNF(I/L)", "PNF_IL");
                                bulk.ColumnMappings.Add("PNF(O/L)", "PNF_OL");
                                bulk.ColumnMappings.Add("OSPT", "OSPT");
                                bulk.ColumnMappings.Add("OSPS", "OSPS");
                                bulk.ColumnMappings.Add("AU_MASK(1st)", "AU_MASK1");
                                bulk.ColumnMappings.Add("AU_MASK(2nd)", "AU_MASK2");
                                bulk.ColumnMappings.Add("AU_PLASMA", "AU_PLASMA");
                                bulk.ColumnMappings.Add("F53 E'less Cu plating", "F53ECU");
                                bulk.ColumnMappings.Add("F53 Vertical E'less Cu plating", "F53VECU");
                                bulk.ColumnMappings.Add("F33 E'less Cu plating", "F33ECU");
                                bulk.ColumnMappings.Add("F52 E'less Cu plating", "F52ECU");
                                bulk.ColumnMappings.Add("F33 SM Cz", "F33SMCZ");
                                bulk.ColumnMappings.Add("F52 SM Cz", "F52SMCZ");
                                bulk.ColumnMappings.Add("FS1 SM Cz", "FS1SMCZ");
                                bulk.ColumnMappings.Add("2D Barcode (CO2)", "BARCODE_CO");
                                bulk.ColumnMappings.Add("2D Barcode (Yag)", "BARCODE_YAG");
                                bulk.ColumnMappings.Add("DFR", "DFR");
                                bulk.ColumnMappings.Add("S_Desmear", "S_DESMEAR");
                                bulk.ColumnMappings.Add("UV_BARCODE", "UV_BARCODE");
                                bulk.ColumnMappings.Add("DES(O/L)", "DES_OL");
                                bulk.ColumnMappings.Add("F42 Des/E-less Cu Plating", "F42_CU");
                                bulk.ColumnMappings.Add("F93 E-less Cu Plating (I/L)", "F93_CU");
                                bulk.ColumnMappings.Add("F94 DES(I/L)", "F9_DESIL");
                                bulk.ColumnMappings.Add("F94 DES(O/L)", "F9_DESOL");
                                bulk.ColumnMappings.Add("Sales Order", "SO_NUMBER");
                                //bulk.ColumnMappings.Add("Schedule Line ID", "SCHEDULE_LINE_ID");
                                bulk.ColumnMappings.Add("Order Date", "ORDER_DATE");

                                bulk.ColumnMappings.Add("INSERT_ID", "INSERT_ID");
                                bulk.ColumnMappings.Add("INSERT_DTTM", "INSERT_DTTM");
                                bulk.ColumnMappings.Add("UPDATE_ID", "UPDATE_ID");
                                bulk.ColumnMappings.Add("UPDATE_DTTM", "UPDATE_DTTM");

                                bulk.WriteToServer(ds.Tables[0]);
                                Console.WriteLine($"WriteToServer: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                            }

                            // LIST에서 해당 버전의 개수가 없으면 
                            // H테이블에서 있는거 버전으로 Y로 만들어야함.. 
                            //
                            //
                            //
                            //
                            //






                            tx.Commit();
                        }
                        //catch (Exception ex)
                        //{
                        //    tx.Rollback();
                        //    Console.WriteLine(ex.StackTrace);
                        //}
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            throw new Exception(ex.Message.ToString());
                           // Console.WriteLine("SQL Error: " + ex.Message);
                           // Console.WriteLine($"SqlBulkCopy 생성 실패: {ex.GetType().Name} - {ex.Message}");
                            //Console.WriteLine("Error Number: " + ex.Number); // 예: -2는 타임아웃
                           // Console.WriteLine($"에러: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        }
                    }
                }



                // 파일 업로드 했다 가정하고
                returnMessage = "파일 업로드가 완료 되었습니다.";
            }

            return new ObjectResult(new { sttaus = "ok", message = returnMessage })
            {
                StatusCode = 200
            };
        }
    }
}
