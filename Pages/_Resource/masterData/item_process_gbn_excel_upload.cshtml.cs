using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
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
    public class item_process_gbn_excel_upload : BasePageModel
    {
        public item_process_gbn_excel_upload()
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
    TH_GUI_ITEM_BY_PROCESS_GUBUN
WHERE
    DIVISION_ID = {terms["group_id"].V}
");

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
	MENU_CODE = 'MD010122'
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
                Params terms = e.Params["terms"];
                DataTable dtResult = this.Search(terms);

                // 디비에서 직접조회해서 DataTable과 파일이름을 매개변수로 넘기면 됨 
                string fileName = $"ITEM_BY_PROCESS_GUBUN_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (dtResult == null || dtResult.Rows.Count == 0)
                {
                    return BadRequest("데이터가 없습니다.");
                }


                return HS.Core.Excel.Download(dtResult, fileName);
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

                DataRow firstRow = ds.Tables[0].Rows[0];

                Params terms = e.Params["terms"];
                string division = terms["group_id"].AsString();

                // !!!!! DIVISION_ID 컬럼 엑셀에서 안넘어오도록 바뀜
                //if (firstRow["DIVISION_ID"].ToString() != division)
                //{
                //    return new ObjectResult(new { status = "error", message = "DIVISION_ID mismatch" })
                //    {
                //        StatusCode = 400 // 또는 403, 상황에 따라 적절한 코드 선택
                //    };
                //}

                // DIVISION_ID 컬럼이 없을 경우 넣어줘야함.
                if (!ds.Tables[0].Columns.Contains("DIVISION_ID"))
                {
                    ds.Tables[0].Columns.Add("DIVISION_ID", typeof(string));

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        row["DIVISION_ID"] = division;
                    }
                }

                // 업로드 하기전에 테이블 전체 삭제하고 다시 올리기
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
                            sSQL.Append(@$"
DELETE FROM TH_GUI_ITEM_BY_PROCESS_GUBUN WHERE DIVISION_ID = '{division}';
");
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
    'MD010122',
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

                                bulk.DestinationTableName = "dbo.TH_GUI_ITEM_BY_PROCESS_GUBUN";

                                bulk.ColumnMappings.Add("DIVISION_ID", "DIVISION_ID");
                                bulk.ColumnMappings.Add("ITEM_CODE", "ITEM_CODE");
                                bulk.ColumnMappings.Add("REVISION", "REVISION");
                                bulk.ColumnMappings.Add("CUSTOMER_NAME", "CUSTOMER_NAME");
                                bulk.ColumnMappings.Add("CATEGORY3", "CATEGORY3");
                                bulk.ColumnMappings.Add("D_LAYER", "D_LAYER");
                                bulk.ColumnMappings.Add("MODEL_NAME", "MODEL_NAME");
                                bulk.ColumnMappings.Add("SHRINKAGE_RATE", "SHRINKAGE_RATE");
                                bulk.ColumnMappings.Add("MAX_LOT_PNL", "MAX_LOT_PNL");
                                bulk.ColumnMappings.Add("PATTERN", "PATTERN");
                                bulk.ColumnMappings.Add("PATTERN_GUBUN", "PATTERN_GUBUN");
                                bulk.ColumnMappings.Add("PROCESS_GUBUN", "PROCESS_GUBUN");
                                bulk.ColumnMappings.Add("LAYUP_TYPE", "LAYUP_TYPE");


                                bulk.WriteToServer(ds.Tables[0]);
                            }
                            tx.Commit();

                        } catch(Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            tx.Rollback();
                            return new ObjectResult(new { status = "error", message = ex.ToString() })
                            {
                                StatusCode = 400 // 또는 403, 상황에 따라 적절한 코드 선택
                            };
                        }
                    }
                }

                

                // 파일 업로드 했다 가정하고
                returnMessage = "파일 업로드가 완료 되었습니다.";
            }

            return new ObjectResult(new { status = "ok", message = returnMessage })
            {
                StatusCode = 200
            };
        }
    }
}
