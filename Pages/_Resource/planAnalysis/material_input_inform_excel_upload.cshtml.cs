using DocumentFormat.OpenXml.Office.Word;
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
//using static GrapeCity.Documents.Pdf.Spec.PdfName;
//using System;

namespace HS.Web.Pages
{
    public class material_input_inform_excel_upload : BasePageModel
    {
        public material_input_inform_excel_upload()
        {
            this.Handler = handler;
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
    TH_TAR_EXPECTED_IN_MATERIAL_L
WHERE 
    1=1
");
            if (terms["group_id"].Length > 0)
            {
                if (terms["group_id"].AsString() == "SPS")
                {
                    sSQL.Append($@"
    AND DIVISION_ID = 'SPS'
");
                }

                if (terms["group_id"].AsString() == "HDI")
                {
                    sSQL.Append($@"
    AND DIVISION_ID = 'HDI'
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
	MENU_CODE = 'MD010120'
    AND DIVISION_ID = {terms["group_id"].V}
order by INSERT_DTTM DESC
");

            return Data.Get(sSQL.ToString()).Tables[0];
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
                ParamList list = e.Params["list"];

                // 엑셀 저장 시 DIVISION 체크해야함
                Vali vali = new Vali(terms);
                vali.Null("group_id", "DIVISION을 선택해주세요.");

                vali.DoneDeco();

                string division = terms["group_id"].AsString();

                // 업로드 하기전에 테이블 전체 삭제하고 다시 올리기

                // ORGANIZATION_ID 컬럼이 없을 경우 넣어줘야함.
                if (!ds.Tables[0].Columns.Contains("ORGANIZATION_ID"))
                {
                    ds.Tables[0].Columns.Add("ORGANIZATION_ID", typeof(string));

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        row["ORGANIZATION_ID"] = "101";
                    }
                }

                // USE_YN 컬럼이 없을 경우 넣어줘야함.
                if (!ds.Tables[0].Columns.Contains("USE_YN"))
                {
                    ds.Tables[0].Columns.Add("USE_YN", typeof(string));

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        row["USE_YN"] = "Y";
                    }
                }

                DataTable dt = ds.Tables[0];

                string[] dateColumns = { "입고일자", "PO_DATE" };
                string[] accountingColumns = { "입고수량", "PO_REMAIN_QTY" };

                // 역순으로 삭제 (안전하게)
                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    var row = dt.Rows[i];

                    var site = row["Site"]?.ToString().Trim();
                    var 입고일자 = row["입고일자"]?.ToString().Trim();
                    var 입고수량 = row["입고수량"]?.ToString().Trim();

                    if (site != "STK" || string.IsNullOrWhiteSpace(입고일자) || string.IsNullOrWhiteSpace(입고수량))
                    {
                        dt.Rows.RemoveAt(i);
                    }
                }

                // 등록일자 컬럼이 없으면 추가
                if (!dt.Columns.Contains("등록일자"))
                {
                    dt.Columns.Add("등록일자", typeof(string)); // 문자열로 선언
                }

                // 오늘 날짜를 YYYYMMDD 형식으로 변환
                string today = DateTime.Today.ToString("yyyyMMdd");

                // 모든 행에 대해 등록일자를 오늘 날짜로 설정
                foreach (DataRow row in dt.Rows)
                {
                    row["등록일자"] = today;
                }



                foreach (DataRow row in dt.Rows)
                {
                    foreach (DataColumn col in dt.Columns)
                    {
                        var value = row[col];
                        var str = value?.ToString().Trim();

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

                        // 회계 형식 컬럼 처리
                        if (accountingColumns.Contains(col.ColumnName))
                        {
                            if (string.IsNullOrWhiteSpace(value?.ToString()))
                            {
                                row[col] = DBNull.Value;
                            }
                            else if (str == "-" || str == "–" || str == "−")
                            {
                                row[col] = 0; // 또는 DBNull.Value, 상황에 따라
                            }
                            else
                            {
                                str = str.Replace("₩", "").Replace(",", "");
                                if (double.TryParse(str, out double parsed))
                                {
                                    row[col] = parsed;
                                }
                                else
                                {
                                    row[col] = DBNull.Value;
                                }
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
                    row["INSERT_ID"] = Cookie<User>.Store.USER_ID; 
                    row["INSERT_DTTM"] = DateTime.Now;
                    row["UPDATE_ID"] = Cookie<User>.Store.USER_ID;
                    row["UPDATE_DTTM"] = DateTime.Now;
                }

                // bulk insert로 짧은시간안에 데이터 넣어주기
                (string dbType, string connection) = Data.GetConnection("Default");

                using (SqlConnection conn = new SqlConnection(connection) )
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            StringBuilder sSQL = new StringBuilder();
                            sSQL.Append(@"
DELETE FROM TH_TAR_EXPECTED_IN_MATERIAL_L WHERE 1=1
");
                            if (division == "SPS")
                            {
                                sSQL.Append($@"
AND DIVISION_ID = 'SPS'
");
                            }
                            else if (division == "HDI")
                            {
                                sSQL.Append($@"
AND DIVISION_ID = 'HDI'
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
    'MD010120',
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


                            using (SqlBulkCopy bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx))
                            {
                                // Timeout 시간 조정
                                bulk.BulkCopyTimeout = 300;

                                bulk.DestinationTableName = "dbo.TH_TAR_EXPECTED_IN_MATERIAL_L";

                                //bulk.ColumnMappings.Add("ORGANIZATION_ID", "ORGANIZATION_ID");
                                bulk.ColumnMappings.Add("사업부", "DIVISION_ID");
                                bulk.ColumnMappings.Add("Site", "PLANT");
                                bulk.ColumnMappings.Add("등록일자", "YYYYMMDD");
                                bulk.ColumnMappings.Add("자재코드", "MATERIAL_CODE");
                                bulk.ColumnMappings.Add("자재명", "MATERIAL_NAME");
                                bulk.ColumnMappings.Add("입고일자", "EXPECTED_IN_DATE");
                                bulk.ColumnMappings.Add("입고수량", "EXPECTED_IN_QTY");
                                bulk.ColumnMappings.Add("PO_NUMBER", "PO_NUMBER");
                                bulk.ColumnMappings.Add("PO_DATE", "PO_DATE");
                                bulk.ColumnMappings.Add("PO_TOTAL_QTY", "PO_TOTAL_QTY");
                                bulk.ColumnMappings.Add("PO_REMAIN_QTY", "PO_REMAIN_QTY");
                                bulk.ColumnMappings.Add("DESCRIPTION", "DESCRIPTION");
                                //bulk.ColumnMappings.Add("USE_YN", "USE_YN");
                                bulk.ColumnMappings.Add("INSERT_ID", "INSERT_ID");
                                bulk.ColumnMappings.Add("INSERT_DTTM", "INSERT_DTTM");
                                bulk.ColumnMappings.Add("UPDATE_ID", "UPDATE_ID");
                                bulk.ColumnMappings.Add("UPDATE_DTTM", "UPDATE_DTTM");

                                //Console.WriteLine(ds.Tables[0]);
                                bulk.WriteToServer(ds.Tables[0]);
                            }
                            tx.Commit();

                        } catch(Exception ex)
                        {
                            tx.Rollback();
                            throw new Exception(ex.Message.ToString());
                           // Console.WriteLine(ex.StackTrace);
                        }
                    }
                }

                returnMessage = "파일 업로드가 완료 되었습니다.";
            }

            return new ObjectResult(new { sttaus = "ok", message = returnMessage })
            {
                StatusCode = 200
            };
        }
    }
}
