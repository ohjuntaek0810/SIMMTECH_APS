//using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class item_by_sales_order : BasePageModel
    {
        public item_by_sales_order(ILogger<item_by_sales_order> logger)
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "search_detail")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchDetail(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("WRK_CLS_CD", "�۾��з��ڵ尡 �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("WRK_CLS_NM", "�۾��з����� �Էµ��� �ʾҽ��ϴ�.");
                //vali.Null("VIEW_YN", "���̱� ���ΰ� �Էµ��� �ʾҽ��ϴ�.");

                vali.DoneDeco();

                this.Save(data);


                // ������ ����
            }

            else if(e.Command == "delete")
            {
                ParamList data = e.Params["data"];


                this.delete(data);
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


            DataTable result = new DataTable();

            result.Columns.Add("Sales Order ID", typeof(string));
            result.Columns.Add("Item ID", typeof(string));
            result.Columns.Add("Demand Type", typeof(string));
            result.Columns.Add("��޵�", typeof(string));
            result.Columns.Add("Order QTY", typeof(int));
            result.Columns.Add("Inventory QTY", typeof(int));
            result.Columns.Add("�����ؼ� ����", typeof(string));
            result.Columns.Add("Scheduled Date", typeof(string));
            result.Columns.Add("�԰� ������", typeof(string));
            result.Columns.Add("���� �����ϼ�", typeof(int));
            result.Columns.Add("�����ؼ���", typeof(int));
            result.Columns.Add("��������", typeof(int));

            result.Rows.Add("SO#1", "MCP19651B00", "MASS", "�Ϲ�", 100, 50, "Y",
                           "2025-01-06", "2025-01-06", 0, 100, 0);
            result.Rows.Add("SO#2", "MCP19651B00", "MASS", "���", 100, 0, "N",
                           "2025-01-08", "2025-01-11", 3, 80, 20);
            result.Rows.Add("SO#3", "MCP19651B00", "MASS", "�ʱ��", 100, 0, "N",
                           "2025-01-08", "2025-01-10", 2, 50, 50);


            return result;
        }

        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchDetail(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            DataTable result = new DataTable();

            result.Columns.Add("Sales Order ID", typeof(string));
            result.Columns.Add("Order QTY", typeof(string));
            result.Columns.Add("Lot ID", typeof(string));
            result.Columns.Add("Lot QTY", typeof(string));
            result.Columns.Add("Pegged QTY", typeof(string));
            result.Columns.Add("Lot Status", typeof(string));
            result.Columns.Add("�����׷� Location", typeof(string));
            result.Columns.Add("�󼼰��� Location", typeof(string));
            result.Columns.Add("Site", typeof(string));
            result.Columns.Add("Dept", typeof(string));
            result.Columns.Add("Scheduled Date", typeof(string));
            result.Columns.Add("���� �ؼ�����", typeof(string));
            result.Columns.Add("Hold ����", typeof(string));
            result.Columns.Add("Hold ���� ������", typeof(string));
            result.Columns.Add("�԰� ������", typeof(string));
            result.Columns.Add("���� ���� �ϼ�", typeof(string));
            result.Columns.Add("���� ����", typeof(string));

            result.Rows.Add("SO#2", "100", "LOT#1", "20", "20", "Run", "FVI", "", "F30", "", "1/8/2025", "Y", "", "1/8/2025", "", "");
            result.Rows.Add("SO#2", "100", "LOT#2", "20", "20", "Hold", "FVI", "", "F30", "", "1/8/2025", "N", "Y", "1/10/2025", "1/11/2025", "3");
            result.Rows.Add("SO#2", "100", "LOT#3", "20", "20", "Wait", "FVI", "", "F30", "", "1/8/2025", "Y", "", "1/8/2025", "", "");
            result.Rows.Add("SO#2", "100", "LOT#4", "20", "20", "Wait", "FVI", "", "F30", "", "1/8/2025", "Y", "", "1/8/2025", "", "");
            result.Rows.Add("SO#2", "100", "LOT#5", "20", "20", "Wait", "FVI", "", "F30", "", "1/8/2025", "Y", "", "1/8/2025", "", "");


            return result;
        }

        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private Params search_chart(Params terms)
        {
            Params result = new();

            return result;
        }


        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            //HS.Web.Proc.SAF_WRK_CLS.Save(data);
        }

        /// <summary>
        /// ������ �׸� ����
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            throw new Exception("�غ����Դϴ�.");

            StringBuilder sSQL = new StringBuilder();

            data.ForEach(D =>
            {
                sSQL.Append($@"
DELETE FROM SI_CODE_GROUP WHERE CMP_CD = {D["CMP_CD"].V} AND GRP_CD = {D["GRP_CD"].V};
");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        /// <summary>
        /// ����� �׸��� ����÷� ��������
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private DataTable SearchGrid(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            string USER_ID = "admin";
            string GRID_ID = terms["grid_id"].AsString();

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
	SUG.COLUMN_NAME AS dataField,
    SUG.COLUMN_NAME AS label,
	SUG.VISIBLE AS visible,
	SUG.WIDTH AS width,
	SUG.FIX AS fixed,
	SUG.EDITABLE AS editable
FROM
	TH_GUI_USER_GRID SUG
WHERE
	1=1
	AND SUG.USER_ID = '{USER_ID}'
	AND SUG.GRID_ID = '{GRID_ID}'
ORDER BY SUG.COLUMN_ORDER
");

            return Data.Get(sSQL.ToString()).Tables[0];

        }



        /// <summary>
        /// �׸��� ����÷� �ɼ� ����
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveGrid(ParamList dataList)
        {
            HS.Web.Proc.SI_GRID.Save(dataList);
        }


        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            if (command == "ExcelDownload")
            {
                //������ ��ȸ�� ������ ���� �ٿ�ε�
                DataTable dtResult = this.Search(e.Params["terms"]);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Lot_Routing_Sequence_" + timestamp);
            }
            else
                return Page();
        }
    }
}
