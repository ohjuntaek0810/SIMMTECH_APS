using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.Json;
//using System;

namespace HS.Web.Pages
{
    public class item_model_search_excel_upload : BasePageModel
    {
        public item_model_search_excel_upload()
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
        /// ��ȸ ���� 
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
    TH_GUI_ITEM_MODEL_SEARCH
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
	MENU_CODE = 'MD010121'
    AND DIVISION_ID = {terms["group_id"].V}
order by INSERT_DTTM DESC
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();
            Console.WriteLine("++ OnPostPage");
            Console.WriteLine(command);

            if (command == "ExcelDownload")
            {
                Params terms = e.Params["terms"];
                
                DataTable dtResult = this.Search(terms);

                // ��񿡼� ������ȸ�ؼ� DataTable�� �����̸��� �Ű������� �ѱ�� �� 
                string fileName = $"ITEM_MODEL_SEARCH_{DateTime.Now:yyyyMMdd_HHmmss}";

                return HS.Core.Excel.Download(dtResult, fileName);
            }
            else
                return Page();
        }

        private IActionResult postFetchHandler(PostArgs e)
        {
            string returnMessage = "";

            if (e.Command == "ExcelDownload")
            {
                Params terms = e.Params["terms"];

                DataTable dtResult = this.Search(terms);

                string fileName = $"ITEM_MODEL_SEARCH_{DateTime.Now:yyyyMMdd_HHmmss}";

                return HS.Core.Excel.Download(dtResult, fileName);
            }


            if (e.Command == "excelUplaod__���251001")
            {
                Console.WriteLine("excelUplaod");
                IFormFile file = e.Files[0];
                DataSet ds = HS.Core.Excel.Import(file.OpenReadStream());
                ParamList excelData = ds.Tables[0].ToParamList();

                DataRow firstRow = ds.Tables[0].Rows[0];

                Params terms = e.Params["terms"];
                string division = terms["group_id"].AsString();

                //DataTable table = ds.Tables[0];

                // !!!!! DIVISION_ID �÷� �������� �ȳѾ������ �ٲ�
                //if (firstRow["DIVISION_ID"].ToString() != division)
                //{
                //    return new ObjectResult(new { status = "error", message = "DIVISION_ID mismatch" })
                //    {
                //        StatusCode = 400 // �Ǵ� 403, ��Ȳ�� ���� ������ �ڵ� ����
                //    };
                //}

                // DIVISION_ID �÷��� ���� ��� �־������.
                if (!ds.Tables[0].Columns.Contains("DIVISION_ID"))
                {
                    ds.Tables[0].Columns.Add("DIVISION_ID", typeof(string));

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        row["DIVISION_ID"] = division;
                    }
                }
                Console.WriteLine("DIVISION complete");

                // ���ε� �ϱ����� ���̺� ��ü �����ϰ� �ٽ� �ø���
                // ������ �ܼ�â�� ����
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var values = string.Join(", ", row.ItemArray.Select(v => v?.ToString() ?? "NULL"));
                    Console.WriteLine(values);
                }
                Console.WriteLine("console complete");

                //�ߺ� �÷��� �����̹�
                HashSet<string> columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataColumn col in ds.Tables[0].Columns)
                {
                    string originalName = col.ColumnName;
                    string newName = originalName;
                    int suffix = 1;

                    while (columnNames.Contains(newName))
                    {
                        newName = $"{originalName}_{suffix++}";
                    }

                    col.ColumnName = newName;
                    columnNames.Add(newName);
                }

                List<string> renamedColumns = ds.Tables[0].Columns.Cast<DataColumn>()
                                           .Select(c => c.ColumnName)
                                           .ToList();

                // bulk 
                (string dbType, string connection) = Data.GetConnection("Default");

                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            StringBuilder sSQL = new StringBuilder();
                            sSQL.Append(@$"
DELETE FROM TH_GUI_ITEM_MODEL_SEARCH WHERE DIVISION_ID = '{division}';
");
                            using (SqlCommand deleteCmd = new SqlCommand(sSQL.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                deleteCmd.ExecuteNonQuery(); // �̰ɷ��ؾ� tx�� ����
                            }

                            // �������� �����ϱ� ���� ���� �� üũ
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
                            // �������� ����
                            using (SqlCommand cmd = new SqlCommand(sSQL2.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                               // cmd.ExecuteNonQuery(); // �̰ɷ��ؾ� tx�� ����
                            }

                            using (SqlBulkCopy bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx))
                            {
                                // Timeout �ð� ����
                                bulk.BulkCopyTimeout = 300;

                                bulk.DestinationTableName = "dbo.TH_GUI_ITEM_MODEL_SEARCH";
                                bulk.ColumnMappings.Add(0, "ITEM_CODE");
                                bulk.ColumnMappings.Add(1, "REV");
                                bulk.ColumnMappings.Add(2, "CUSTOMER");
                                bulk.ColumnMappings.Add(3, "CATEGORY3");
                                bulk.ColumnMappings.Add(4, "LAYER");
                                bulk.ColumnMappings.Add(5, "MODEL_NAME");
                                bulk.ColumnMappings.Add(6, "THICK");
                                bulk.ColumnMappings.Add(7, "TOL+");
                                bulk.ColumnMappings.Add(8, "TOL-");
                                bulk.ColumnMappings.Add(9, "FINISH");
                                bulk.ColumnMappings.Add(10, "ROW");
                                bulk.ColumnMappings.Add(11, "COLUMN");
                                bulk.ColumnMappings.Add(12, "BLK_QTY");
                                bulk.ColumnMappings.Add(13, "UP");
                                bulk.ColumnMappings.Add(14, "DATECODE");
                                bulk.ColumnMappings.Add(15, "SEQ");
                                bulk.ColumnMappings.Add(16, "HOLE+TYPE");
                                bulk.ColumnMappings.Add(17, "BIT_SIZE");
                                bulk.ColumnMappings.Add(18, "LAND_SIZE");
                                //bulk.ColumnMappings.Add(19, "PTH"); // PTH �ߺ����� ��
                                bulk.ColumnMappings.Add(20, "VIA_BIT");
                                bulk.ColumnMappings.Add(21, "COUNT_ARRAY");
                                bulk.ColumnMappings.Add(22, "UNIT_SIZE_X");
                                bulk.ColumnMappings.Add(23, "UNIT_SIZE_Y");
                                bulk.ColumnMappings.Add(24, "ARRAY_SIZE_X");
                                bulk.ColumnMappings.Add(25, "ARRAY_SIZE_Y");
                                bulk.ColumnMappings.Add(26, "ARRANGEMENT_X");
                                bulk.ColumnMappings.Add(27, "ARRANGEMENT_Y");
                                bulk.ColumnMappings.Add(28, "PANEL_SIZE_X");
                                bulk.ColumnMappings.Add(29, "PANEL_SIZE_Y");
                                bulk.ColumnMappings.Add(30, "SPACE_X");
                                bulk.ColumnMappings.Add(31, "SPACE_Y");
                                bulk.ColumnMappings.Add(32, "UNIT_NO");
                                bulk.ColumnMappings.Add(33, "STRIP_NO");
                                bulk.ColumnMappings.Add(34, "CUSTOMER_NO");
                                bulk.ColumnMappings.Add(35, "SCALE_X1");
                                bulk.ColumnMappings.Add(36, "SCALE_X2");
                                bulk.ColumnMappings.Add(37, "SCALE_Y1");
                                bulk.ColumnMappings.Add(38, "SCALE_Y2");
                                bulk.ColumnMappings.Add(39, "SR_LNK");
                                bulk.ColumnMappings.Add(40, "CCL");
                                bulk.ColumnMappings.Add(41, "PPG1");
                                bulk.ColumnMappings.Add(42, "CF1");
                                bulk.ColumnMappings.Add(43, "PPG2");
                                bulk.ColumnMappings.Add(44, "CF2");
                                bulk.ColumnMappings.Add(45, "PPG3");
                                bulk.ColumnMappings.Add(46, "CF3");
                                bulk.ColumnMappings.Add(47, "PPG4");
                                bulk.ColumnMappings.Add(48, "CF4");
                                bulk.ColumnMappings.Add(49, "FINGER_WS");
                                bulk.ColumnMappings.Add(50, "FINGER_PITCH");
                                bulk.ColumnMappings.Add(51, "FINGER_AW");
                                bulk.ColumnMappings.Add(52, "FINGER_DELTA");
                                bulk.ColumnMappings.Add(53, "TRACE_WS");
                                bulk.ColumnMappings.Add(54, "TRACE_PITCH");
                                bulk.ColumnMappings.Add(55, "TRACE_AW");
                                bulk.ColumnMappings.Add(56, "TRACE_DELTA");
                                bulk.ColumnMappings.Add(57, "BALL_PAD");
                                bulk.ColumnMappings.Add(58, "BALL_PAD_AW");
                                bulk.ColumnMappings.Add(59, "BALL_SMO");
                                bulk.ColumnMappings.Add(60, "BALL_SMO_AW");
                                bulk.ColumnMappings.Add(61, "BUMP_PAD");
                                bulk.ColumnMappings.Add(62, "BUMP_PAD_AW");
                                bulk.ColumnMappings.Add(63, "BUMP_SMO");
                                bulk.ColumnMappings.Add(64, "BUMP_SMO_AW");
                                bulk.ColumnMappings.Add(65, "THICKNESS");
                                bulk.ColumnMappings.Add(66, "SOFT_NI");
                                bulk.ColumnMappings.Add(67, "SOFT_AU");
                                bulk.ColumnMappings.Add(68, "HARD_NI");
                                bulk.ColumnMappings.Add(69, "HARD_AU");
                                bulk.ColumnMappings.Add(70, "AU_ELECTROLESS");
                                bulk.ColumnMappings.Add(71, "NI_ELECTROLESS");
                                bulk.ColumnMappings.Add(72, "PD");
                                bulk.ColumnMappings.Add(73, "OSP");
                                bulk.ColumnMappings.Add(74, "SOP");
                                bulk.ColumnMappings.Add(75, "ORDER_REPEAT");
                                bulk.ColumnMappings.Add(76, "ORDER_TYPE");
                                bulk.ColumnMappings.Add(77, "APPROVAL_DATE");
                                bulk.ColumnMappings.Add(78, "DDR_TYPE");
                                bulk.ColumnMappings.Add(79, "CUSTOMIZED");
                                bulk.ColumnMappings.Add(80, "LAYUP_TYPE");
                                bulk.ColumnMappings.Add(81, "PART_NO");
                                bulk.ColumnMappings.Add(82, "FACTORY");
                                bulk.ColumnMappings.Add(83, "FIRST_INPUT");
                                bulk.ColumnMappings.Add(84, "FIRST_MASS");
                                bulk.ColumnMappings.Add(85, "PANEL_USAGE");
                                bulk.ColumnMappings.Add(86, "CCL_THICK");
                                bulk.ColumnMappings.Add(87, "PTH");
                                bulk.ColumnMappings.Add(88, "BVH");
                                bulk.ColumnMappings.Add(89, "SURFACE");
                                bulk.ColumnMappings.Add(90, "GUBUN");
                                bulk.ColumnMappings.Add(91, "MFG_CATEGORY");
                                bulk.ColumnMappings.Add(92, "SEQ_MIN");
                                bulk.ColumnMappings.Add(93, "HOLE_TYPE");
                                //bulk.ColumnMappings.Add(94, "PART_NO");
                                bulk.ColumnMappings.Add(95, "BBT_TYPE");
                                bulk.ColumnMappings.Add(96, "BALL_PAD_COUNT");
                                bulk.ColumnMappings.Add(97, "DIVISION_ID");

                                bulk.WriteToServer(ds.Tables[0]);


                            }

                            StringBuilder sSQL3 = new StringBuilder();
                            sSQL3.Append($@"
update TH_GUI_ITEM_MODEL_SEARCH 
set REV  = FORMAT(CONVERT(INT, REV), '000')
");
                            // REV �÷� ������Ʈ
                            using (SqlCommand cmd = new SqlCommand(sSQL3.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                cmd.ExecuteNonQuery(); // �̰ɷ��ؾ� tx�� ����
                            }


                            tx.Commit();

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            tx.Rollback();
                            return new ObjectResult(new { status = "error", message = ex.ToString() })
                            {
                                StatusCode = 400 // �Ǵ� 403, ��Ȳ�� ���� ������ �ڵ� ����
                            };
                        }
                    }
                }



                // ���� ���ε� �ߴ� �����ϰ�
                returnMessage = "���� ���ε尡 �Ϸ� �Ǿ����ϴ�.";
            }


            if (e.Command == "excelUplaod")
            {
                IFormFile file = e.Files[0];
                //DataSet ds = HS.Core.Excel.Import(file.OpenReadStream());
                //ParamList excelData = ds.Tables[0].ToParamList();
                //DataTable table = ds.Tables[0];

                //DataRow firstRow = ds.Tables[0].Rows[0];

                Params terms = e.Params["terms"];
                string division = terms["group_id"].AsString();

                string filePath = SaveUploadedFile(file); // IFormFile�� �ӽ� ��ο� ����
                string connStr = "Provider=Microsoft.ACE.OLEDB.12.0;" +
                                 $"Data Source={filePath};" +
                                 "Extended Properties='Excel 12.0 Xml;HDR=NO;IMEX=1'";

                DataSet ds = new DataSet();

                using (OleDbConnection conn = new OleDbConnection(connStr))
                {
                    conn.Open();

                    DataTable schemaTable = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                    // ù ��° ��Ʈ �̸� ��������
                    string sheetName = schemaTable.Rows[0]["TABLE_NAME"].ToString();

                    OleDbDataAdapter adapter = new OleDbDataAdapter($"SELECT * FROM [{sheetName}]", conn);
                    adapter.Fill(ds);
                }

                DataTable table = ds.Tables[0];

                if (table.Rows.Count > 0)
                {
                    ds.Tables[0].Rows[0].Delete(); // ù������
                    ds.Tables[0].AcceptChanges();  // ���� Ȯ��
                }


                // !!!!! DIVISION_ID �÷� �������� �ȳѾ������ �ٲ�
                //if (firstRow["DIVISION_ID"].ToString() != division)
                //{
                //    return new ObjectResult(new { status = "error", message = "DIVISION_ID mismatch" })
                //    {
                //        StatusCode = 400 // �Ǵ� 403, ��Ȳ�� ���� ������ �ڵ� ����
                //    };
                //}

                // �ߺ� �÷��� �����̹�
                //HashSet<string> columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                //foreach (DataColumn col in table.Columns)
                //{
                //    string originalName = col.ColumnName;
                //    string newName = originalName;
                //    int suffix = 1;

                //    while (columnNames.Contains(newName))
                //    {
                //        newName = $"{originalName}_{suffix++}";
                //    }

                //    col.ColumnName = newName;
                //    columnNames.Add(newName);
                //}

                //List<string> renamedColumns = table.Columns.Cast<DataColumn>()
                //                           .Select(c => c.ColumnName)
                //                           .ToList();


                // DIVISION_ID �÷��� ���� ��� �־������.
                if (!ds.Tables[0].Columns.Contains("DIVISION_ID"))
                {
                    ds.Tables[0].Columns.Add("DIVISION_ID", typeof(string));

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        row["DIVISION_ID"] = division;
                    }
                }

                ParamList list = e.Params["list"];

                // ���ε� �ϱ����� ���̺� ��ü �����ϰ� �ٽ� �ø���
                // ��¥ ���� ���� ���� �ʿ�.
                // ����� �� ��ϳ��� �־������
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
DELETE FROM TH_GUI_ITEM_MODEL_SEARCH WHERE 1=1 AND DIVISION_ID = '{division}'
");
                            using (SqlCommand deleteCmd = new SqlCommand(sSQL.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                deleteCmd.ExecuteNonQuery(); // �̰ɷ��ؾ� tx�� ����
                            }

                            // �������� �����ϱ� ���� ���� �� üũ
                            string uploadedFileName = file.FileName;

                            // division �߰� �ʿ�
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
    'MD010121',
    '{Cookie<User>.Store.USER_ID}',
    GETDATE()
)
");
                            // �������� ����
                            using (SqlCommand cmd = new SqlCommand(sSQL2.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                cmd.ExecuteNonQuery(); // �̰ɷ��ؾ� tx�� ����
                            }


                            using (SqlBulkCopy bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx))
                            {
                                // Timeout �ð� ����
                                bulk.BulkCopyTimeout = 300;

                                bulk.DestinationTableName = "dbo.TH_GUI_ITEM_MODEL_SEARCH";

                                bulk.ColumnMappings.Add(0, "ITEM_CODE");
                                bulk.ColumnMappings.Add(1, "REV");
                                bulk.ColumnMappings.Add(2, "CUSTOMER");
                                bulk.ColumnMappings.Add(3, "CATEGORY3");
                                bulk.ColumnMappings.Add(4, "LAYER");
                                bulk.ColumnMappings.Add(5, "MODEL_NAME");
                                bulk.ColumnMappings.Add(6, "THICK");
                                bulk.ColumnMappings.Add(7, "TOL+");
                                bulk.ColumnMappings.Add(8, "TOL-");
                                bulk.ColumnMappings.Add(9, "FINISH");
                                bulk.ColumnMappings.Add(10, "ROW");
                                bulk.ColumnMappings.Add(11, "COLUMN");
                                bulk.ColumnMappings.Add(12, "BLK_QTY");
                                bulk.ColumnMappings.Add(13, "UP");
                                bulk.ColumnMappings.Add(14, "DATECODE");
                                bulk.ColumnMappings.Add(15, "SEQ");
                                bulk.ColumnMappings.Add(16, "HOLE+TYPE");
                                bulk.ColumnMappings.Add(17, "BIT_SIZE");
                                bulk.ColumnMappings.Add(18, "LAND_SIZE");
                                //bulk.ColumnMappings.Add(19, "PTH"); // PTH �ߺ����� ��
                                bulk.ColumnMappings.Add(20, "VIA_BIT");
                                bulk.ColumnMappings.Add(21, "COUNT_ARRAY");
                                bulk.ColumnMappings.Add(22, "UNIT_SIZE_X");
                                bulk.ColumnMappings.Add(23, "UNIT_SIZE_Y");
                                bulk.ColumnMappings.Add(24, "ARRAY_SIZE_X");
                                bulk.ColumnMappings.Add(25, "ARRAY_SIZE_Y");
                                bulk.ColumnMappings.Add(26, "ARRANGEMENT_X");
                                bulk.ColumnMappings.Add(27, "ARRANGEMENT_Y");
                                bulk.ColumnMappings.Add(28, "PANEL_SIZE_X");
                                bulk.ColumnMappings.Add(29, "PANEL_SIZE_Y");
                                bulk.ColumnMappings.Add(30, "SPACE_X");
                                bulk.ColumnMappings.Add(31, "SPACE_Y");
                                bulk.ColumnMappings.Add(32, "UNIT_NO");
                                bulk.ColumnMappings.Add(33, "STRIP_NO");
                                bulk.ColumnMappings.Add(34, "CUSTOMER_NO");
                                bulk.ColumnMappings.Add(35, "SCALE_X1");
                                bulk.ColumnMappings.Add(36, "SCALE_X2");
                                bulk.ColumnMappings.Add(37, "SCALE_Y1");
                                bulk.ColumnMappings.Add(38, "SCALE_Y2");
                                bulk.ColumnMappings.Add(39, "SR_LNK");
                                bulk.ColumnMappings.Add(40, "CCL");
                                bulk.ColumnMappings.Add(41, "PPG1");
                                bulk.ColumnMappings.Add(42, "CF1");
                                bulk.ColumnMappings.Add(43, "PPG2");
                                bulk.ColumnMappings.Add(44, "CF2");
                                bulk.ColumnMappings.Add(45, "PPG3");
                                bulk.ColumnMappings.Add(46, "CF3");
                                bulk.ColumnMappings.Add(47, "PPG4");
                                bulk.ColumnMappings.Add(48, "CF4");
                                bulk.ColumnMappings.Add(49, "FINGER_WS");
                                bulk.ColumnMappings.Add(50, "FINGER_PITCH");
                                bulk.ColumnMappings.Add(51, "FINGER_AW");
                                bulk.ColumnMappings.Add(52, "FINGER_DELTA");
                                bulk.ColumnMappings.Add(53, "TRACE_WS");
                                bulk.ColumnMappings.Add(54, "TRACE_PITCH");
                                bulk.ColumnMappings.Add(55, "TRACE_AW");
                                bulk.ColumnMappings.Add(56, "TRACE_DELTA");
                                bulk.ColumnMappings.Add(57, "BALL_PAD");
                                bulk.ColumnMappings.Add(58, "BALL_PAD_AW");
                                bulk.ColumnMappings.Add(59, "BALL_SMO");
                                bulk.ColumnMappings.Add(60, "BALL_SMO_AW");
                                bulk.ColumnMappings.Add(61, "BUMP_PAD");
                                bulk.ColumnMappings.Add(62, "BUMP_PAD_AW");
                                bulk.ColumnMappings.Add(63, "BUMP_SMO");
                                bulk.ColumnMappings.Add(64, "BUMP_SMO_AW");
                                bulk.ColumnMappings.Add(65, "THICKNESS");
                                bulk.ColumnMappings.Add(66, "SOFT_NI");
                                bulk.ColumnMappings.Add(67, "SOFT_AU");
                                bulk.ColumnMappings.Add(68, "HARD_NI");
                                bulk.ColumnMappings.Add(69, "HARD_AU");
                                bulk.ColumnMappings.Add(70, "AU_ELECTROLESS");
                                bulk.ColumnMappings.Add(71, "NI_ELECTROLESS");
                                bulk.ColumnMappings.Add(72, "PD");
                                bulk.ColumnMappings.Add(73, "OSP");
                                bulk.ColumnMappings.Add(74, "SOP");
                                bulk.ColumnMappings.Add(75, "ORDER_REPEAT");
                                bulk.ColumnMappings.Add(76, "ORDER_TYPE");
                                bulk.ColumnMappings.Add(77, "APPROVAL_DATE");
                                bulk.ColumnMappings.Add(78, "DDR_TYPE");
                                bulk.ColumnMappings.Add(79, "CUSTOMIZED");
                                bulk.ColumnMappings.Add(80, "LAYUP_TYPE");
                                bulk.ColumnMappings.Add(81, "PART_NO");
                                bulk.ColumnMappings.Add(82, "FACTORY");
                                bulk.ColumnMappings.Add(83, "FIRST_INPUT");
                                bulk.ColumnMappings.Add(84, "FIRST_MASS");
                                bulk.ColumnMappings.Add(85, "PANEL_USAGE");
                                bulk.ColumnMappings.Add(86, "CCL_THICK");
                                bulk.ColumnMappings.Add(87, "PTH");
                                bulk.ColumnMappings.Add(88, "BVH");
                                bulk.ColumnMappings.Add(89, "SURFACE");
                                bulk.ColumnMappings.Add(90, "GUBUN");
                                bulk.ColumnMappings.Add(91, "MFG_CATEGORY");
                                bulk.ColumnMappings.Add(92, "SEQ_MIN");
                                bulk.ColumnMappings.Add(93, "HOLE_TYPE");
                                //bulk.ColumnMappings.Add(94, "PART_NO");
                                bulk.ColumnMappings.Add(95, "BBT_TYPE");
                                bulk.ColumnMappings.Add(96, "BALL_PAD_COUNT");
                                bulk.ColumnMappings.Add(97, "DIVISION_ID");


                                bulk.WriteToServer(ds.Tables[0]);
                            }

                            StringBuilder sSQL3 = new StringBuilder();
                            sSQL3.Append($@"
update TH_GUI_ITEM_MODEL_SEARCH
set REV  = FORMAT(CONVERT(INT, REV), '000')
");
                            // REV �÷� ������Ʈ
                            using (SqlCommand cmd = new SqlCommand(sSQL3.ToString(), conn, tx))
                            {
                                //HS.Web.Common.Data.Execute(sSQL.ToString());
                                cmd.ExecuteNonQuery(); // �̰ɷ��ؾ� tx�� ����
                            }

                            tx.Commit();

                        } catch(Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            tx.Rollback();
                            //throw new Exception(ex.ToString());
                            return new ObjectResult(new { status = "error", message = ex.ToString() })
                            {
                                StatusCode = 400 // �Ǵ� 403, ��Ȳ�� ���� ������ �ڵ� ����
                            };


                        }
                    }
                }

                

                // ���� ���ε� �ߴ� �����ϰ�
                returnMessage = "���� ���ε尡 �Ϸ� �Ǿ����ϴ�.";
            }

            return new ObjectResult(new { status = "ok", message = returnMessage })
            {
                StatusCode = 200
            };
        }

        private string SaveUploadedFile(IFormFile file)
        {
            // �ӽ� ���� ��� ����
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));

            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return tempPath; // ����� ���� ��� ��ȯ
        }


    }
}
