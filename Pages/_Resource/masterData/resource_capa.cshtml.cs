using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class lot_routing_capa : BasePageModel
    {
        private readonly ILogger<lot_routing_sequence> _logger;

        public lot_routing_capa(ILogger<lot_routing_sequence> logger)
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
            _logger = logger;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "search_chart")
            {
                Params terms = e.Params["terms"];

                toClient = this.search_chart(terms);
            }

            else if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
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

            sSQL.Append(@"
SELECT 
	GROUP_NAME AS ""GROUP""
	, MACHINE_ID AS RESOURCE_ID
	, MACHINE_CODE AS RESOURCE_CODE
	, MACHINE_NAME AS RESOURCE_NAME
    , CLASS_CODE AS CLASS_CODE
	, CLASS_NAME AS CLASS_NAME
	, DEPT_CODE AS DEPT_CODE
	, DEPT_NAME AS DEPT_NAME
	, VIRTUAL 
	, MAIN 
	, ""USE""
	, APS_USE
	, ECIM
	, OUTSOURCING
--	, PROCESS_TIME
	, TACT_TIME
	, MONTHLY_CAPA AS ""CAPA/MONTH""
	, DAILY_CAPA AS ""CAPA/DAY""
	, OPERATION_RATE
	, DOWN_TIME
	, DEPT_GROUP
	, RESOURCE_GROUP
	, APS_RESOURCE_GROUP
	, MAKER
	, APPLY_OPTION
	, FACTORY_FLOOR
	, APS_SITE
	, THICKNESS_MIN
	, THICKNESS_MAX
	, XWPNL_MIN
	, XWPNL_MAX
	, YWPNL_MIN
	, YWPNL_MAX
	, REV_DATE
	, UP_DATE
	, UPDATE_ID
FROM 
	dbo.VW_TAR_RESOURCE_MASTER_NEW
WHERE
	1=1
");
            /*
             * ������ ����
             */
            if (terms["group_id"].Length > 0 ) // �����(�׷�) ����
            {
                sSQL.Append($@"
AND GROUP_NAME = {terms["group_id"].V}
");
            }

            if (terms["app_use"].Length > 0) // APP_USE ����
            {
                sSQL.Append($@"
AND [USE] = {terms["app_use"].V}
");
            }


            _logger.LogInformation(">>> sql Query = {}", sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
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
