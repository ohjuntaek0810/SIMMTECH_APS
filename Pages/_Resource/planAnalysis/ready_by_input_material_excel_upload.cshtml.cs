using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
//using GrapeCity.DataVisualization.Chart;
//using GrapeCity.DataVisualization.TypeScript;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.Json;
//using System;

namespace HS.Web.Pages
{
    public class ready_by_input_material_excel_upload : BasePageModel
    {
        public ready_by_input_material_excel_upload()
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
    TH_TAR_READY_BY_INPUT_MATERIAL_L
WHERE
    1=1
");
            if (terms["group_id"].Length > 0)
            {
                if (terms["group_id"].AsString() == "SPS")
                {
                    sSQL.Append($@"
    AND PRODUCT_GROUP_CODE = 'S'
");
                }

                if (terms["group_id"].AsString() == "HDI")
                {
                    sSQL.Append($@"
    AND PRODUCT_GROUP_CODE = 'H'
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
	MENU_CODE = 'PA010204'
    AND DIVISION_ID = {terms["group_id"].V}
order by INSERT_DTTM DESC
");

            return Data.Get(sSQL.ToString()).Tables[0];
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

                //HS.Web.Proc.TH_TAR_READY_BY_INPUT_MATERIAL_L.Save(excelData);

                // VERSION 컬럼이 없을 경우 넣어줘야함.
                //if (!ds.Tables[0].Columns.Contains("VERSION"))
                //{
                //    ds.Tables[0].Columns.Add("VERSION", typeof(string));

                //    foreach(DataRow row in ds.Tables[0].Rows)
                //    {
                //        row["VERSION"] = "-";
                //    }
                //}

                DataTable dt = ds.Tables[0];

                string[] dateColumns = { "NEW_START_DATE", "NEW_DATE", "MFG_DATE", "EXPIRATION_DATE", "투입" };

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
                            }
                            else if (double.TryParse(value.ToString(), out double oaDate))
                            {
                                row[col] = DateTime.FromOADate(oaDate);
                            }
                            else
                            {
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
                    if (row["NEW_START_DATE"].ToString() != "")
                    {
                        row["NEW_START_DATE"] = DateTime.Parse(Convert.ToDateTime(row["NEW_START_DATE"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                    } else
                    {
                        row["NEW_START_DATE"] = DBNull.Value;
                    }
                    if (row["NEW_DATE"].ToString() != "")
                    {
                        row["NEW_DATE"] = DateTime.Parse(Convert.ToDateTime(row["NEW_DATE"].ToString()).ToString("yyyy-MM-dd HH:mm:ss"));
                    } else
                    {
                        row["NEW_DATE"] = DBNull.Value;
                    }
                    if (division == "SPS")
                    {
                        row["PRODUCT_GROUP_CODE"] = "S";
                    }
                    else if (division == "HDI")
                    {
                        row["PRODUCT_GROUP_CODE"] = "H";
                    }

                    row["INSERT_ID"] = Cookie<User>.Store.USER_ID; 
                    row["INSERT_DTTM"] = DateTime.Now;
                    row["UPDATE_ID"] = Cookie<User>.Store.USER_ID;
                    row["UPDATE_DTTM"] = DateTime.Now;
                }


                // 데이터 콘솔창에 띄어보기
                //foreach (DataRow row in ds.Tables[0].Rows)
                //{
                //    var values = string.Join(", ", row.ItemArray.Select(v => v?.ToString() ?? "NULL"));
                //    Console.WriteLine(values);
                //}

                // bulk 
                (string dbType, string connection) = Data.GetConnection("Default");

                using (SqlConnection conn = new SqlConnection(connection) )
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            StringBuilder sSQL = new StringBuilder();
                            //  선택받은 DIVISION에 해당하는것만 삭제하고 다시 올려야함.
                            sSQL.Append(@"
DELETE FROM TH_TAR_READY_BY_INPUT_MATERIAL_L WHERE 1=1
");
                            if (division == "SPS")
                            {
                                sSQL.Append($@"
AND PRODUCT_GROUP_CODE = 'S'
");
                            }
                            else if (division == "HDI")
                            {
                                sSQL.Append($@"
AND PRODUCT_GROUP_CODE = 'H'
");
                            }


                            using (SqlCommand deleteCmd = new SqlCommand(sSQL.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                deleteCmd.ExecuteNonQuery(); // 이걸로해야 tx가 먹음
                            }

                            // 파일정보 저장하기 위해 파일 명 체크
                            string uploadedFileName = file.FileName;

                            StringBuilder sSQL2 = new StringBuilder();
                            sSQL2.Append($@"
INSERT INTO TH_GUI_UPLOAD_FILE (
    DIVISION_ID,
	FILE_NAME,
	MENU_CODE,
	INSERT_ID,
	INSERT_DTTM
) VALUES (
    '{division}',
    '{uploadedFileName}',
    'PA010204',
    '{Cookie<User>.Store.USER_ID}',
    GETDATE()
)
");
                            // 파일정보 저장
                            using (SqlCommand cmd = new SqlCommand(sSQL2.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                cmd.ExecuteNonQuery(); // 이걸로해야 tx가 먹음
                            }

                            StringBuilder updateSQL = new StringBuilder();
                            updateSQL.Append($@"UPDATE TH_TAR_READY_BY_INPUT_MATERIAL_H SET USE_YN = 'N' WHERE DIVISION_ID = '{division}'");

                            using (SqlCommand updateCmd = new SqlCommand(updateSQL.ToString(), conn, tx))
                            {
                                Console.WriteLine($"updateCmd: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                updateCmd.ExecuteNonQuery();
                            }



                            // 헤더에 버전 넣기
                            StringBuilder sSQL3 = new StringBuilder();

                            sSQL3.Append($@"
insert into [TH_TAR_READY_BY_INPUT_MATERIAL_H]
select 
	'101' AS ORGANIZATION_ID,
	'{division}' AS DIVISION_ID,
	'{division}' + '_' + format(getdate(), 'yyyyMMdd') + '_' + format((select MAX(VER_SN) + 1 FROM TH_TAR_READY_BY_INPUT_MATERIAL_H), '000') AS VERSION,
	format(getdate(), 'yyyy-MM-dd') AS CREATE_DATE,
	(select MAX(VER_SN) + 1 FROM TH_TAR_READY_BY_INPUT_MATERIAL_H) AS VER_SN,
	'AVAILABLE' AS VERSION_STATUS,
	'Y' AS USE_YN,
	'MP_250319A' As MPS_PLAN_ID,
    'MP_250319A_SPS_20250704_001' AS READY_BY_INPUT_VERSION,
	null AS DESCR,
	'admin' AS INSERT_ID,
	getdate() AS INSERT_DTTM,
	'admin' AS UPDATE_ID,
	getdate() AS UPDATE_DTTM
");
                            using (SqlCommand insertCmd = new SqlCommand(sSQL3.ToString(), conn, tx))
                            {
                                Console.WriteLine($"insertCmd: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                                //HS.Web.Common.Data.Execute(sSQL2.ToString());
                                insertCmd.ExecuteNonQuery();
                            }



                            // INSERT 후 VERSION 가져오기
                            StringBuilder sSQL4 = new StringBuilder();
                            sSQL4.Append($@"
SELECT TOP 1 VERSION 
FROM TH_TAR_READY_BY_INPUT_MATERIAL_H 
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












                            using (SqlBulkCopy bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx))
                            {
                                // Timeout 시간 조정
                                bulk.BulkCopyTimeout = 300;

                                bulk.DestinationTableName = "dbo.TH_TAR_READY_BY_INPUT_MATERIAL_L";

                                bulk.ColumnMappings.Add("VERSION", "VERSION");
                                bulk.ColumnMappings.Add("GUBUN", "GUBUN");
                                bulk.ColumnMappings.Add("PRODUCT_GROUP_CODE", "PRODUCT_GROUP_CODE");
                                bulk.ColumnMappings.Add("ASSEMBLY_ITEM_CODE", "ASSEMBLY_ITEM_CODE");
                                bulk.ColumnMappings.Add("CUSTOMER_NAME", "CUSTOMER_NAME");
                                bulk.ColumnMappings.Add("MODEL_NAME", "MODEL_NAME");
                                bulk.ColumnMappings.Add("NEW_START_DATE", "NEW_START_DATE");
                                bulk.ColumnMappings.Add("NEW_DATE", "NEW_DATE");
                                bulk.ColumnMappings.Add("ASSY_QTY", "ASSY_QTY");
                                bulk.ColumnMappings.Add("ASSY_M2", "ASSY_M2");
                                bulk.ColumnMappings.Add("COMPONET_ITEM_CODE", "COMPONET_ITEM_CODE");
                                bulk.ColumnMappings.Add("COMPONENT_ITEM", "COMPONENT_ITEM");
                                bulk.ColumnMappings.Add("COMPONENT_QUANTITY", "COMPONENT_QUANTITY");
                                bulk.ColumnMappings.Add("EXTENDED_QUANTITY", "EXTENDED_QUANTITY");
                                bulk.ColumnMappings.Add("COMP_ITEM_TYPE", "COMP_ITEM_TYPE");
                                bulk.ColumnMappings.Add("UOM", "UOM");
                                bulk.ColumnMappings.Add("LAYER", "LAYER");
                                bulk.ColumnMappings.Add("CATEGORY_NAME", "CATEGORY_NAME");
                                bulk.ColumnMappings.Add("MFG_DATE", "MFG_DATE");
                                bulk.ColumnMappings.Add("EXPIRATION_DATE", "EXPIRATION_DATE");
                                bulk.ColumnMappings.Add("EXPIRATION_DAY", "EXPIRATION_DAY");
                                bulk.ColumnMappings.Add("CTYPE", "CTYPE");
                                bulk.ColumnMappings.Add("공법구분", "PATTERN");
                                bulk.ColumnMappings.Add("Layup Type", "LAY_UP_TYPE");
                                bulk.ColumnMappings.Add("Shirink Rate", "SHRINKAGE_REATE");
                                bulk.ColumnMappings.Add("확정수량", "CONFIRMED_QTY");
                                bulk.ColumnMappings.Add("투입", "INPUT_DATE");
                                bulk.ColumnMappings.Add("LT", "LEAD_TIME");
                                bulk.ColumnMappings.Add("STD LT", "STD_LEAD_TIME");
                                bulk.ColumnMappings.Add("INSERT_ID", "INSERT_ID");
                                bulk.ColumnMappings.Add("INSERT_DTTM", "INSERT_DTTM");
                                bulk.ColumnMappings.Add("UPDATE_ID", "UPDATE_ID");
                                bulk.ColumnMappings.Add("UPDATE_DTTM", "UPDATE_DTTM");                               

                                bulk.WriteToServer(ds.Tables[0]);
                            }
                            tx.Commit();

                        } catch(Exception ex)
                        {
                           
                            Console.WriteLine(ex.StackTrace);
                            tx.Rollback();
                            throw new Exception(ex.Message.ToString());
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
