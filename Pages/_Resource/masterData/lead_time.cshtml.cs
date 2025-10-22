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
    public class lead_time : BasePageModel
    {

        public lead_time()
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

            else if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];

                // ������ ����
                this.Save(dataList);
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
select	A.ORGANIZATION_ID, 
        A.DIVISION_ID AS ""GROUP"", 
        A.DEPARTMENT_CLASS_CODE AS ""CLASS CODE"", 
        A.DEPARTMENT_CLASS_NAME AS ""CLASS NAME"", 
        A.DEPARTMENT_ID, 
        A.DEPARTMENT_CODE AS ""DEPARTMENT CODE"", 
        B.DEPARTMENT_NAME AS ""DEPARTMENT NAME"", 
		PLAN_PROCESSING_TIME_HR AS ""PROCESS(PLNA L/T)"", 
		PLAN_ESSENTIAL_WAITING_TIME_HR AS ""READY(PLAN L/T)"", 
		PLAN_OPER_WAITING_TIME_HR AS ""WAIT(PLAN L/T)"",  -- ��ȹLT (����, �ʼ����, ���)		
		STD_AVG_DEPT_PROCESS_TIME_HR AS ""PROCESS(STD L/T)"",							 -- ǥ��LT (����, ���)	
		STD_AVG_DEPT_WAITING_TIME_HR AS ""WAIT(STD L/T)"",							 -- ǥ��LT (����, ���)	
		ACT_DEPT_REV_AVG_PROCESS_TIME_HR AS ""PROCESS AVG"",	-- ���� ����LT ���(�̻�ġ ���� ��)
		ACT_DEPT_ORG_AVG_PROCESS_TIME_HR AS ""PROCESS AVG (ALL)"",	-- ���� ����LT ���(�̻�ġ ���� ��)
		ACT_MED_PROCESS_TIME_HR AS ""PROCESS MED"",			-- ���� ����LT MEDIAN
		ACT_DEPT_MIN_PROCESS_TIME_HR AS ""PROCESS MIN"",		-- ���� ����LT �ּҰ�
		ACT_DEPT_MAX_PROCESS_TIME_HR AS ""PROCESS MAX"",		-- ���� ����LT �ִ밪, 
		ACT_DEPT_STDEV_PROCESS_TIME_HR AS ""PROCESS DEV"",		-- ���� ����LT ǥ������
		ACT_DEPT_REV_AVG_WAITING_TIME_HR AS ""WAIT AVG"",	-- ��� ����LT ���(�̻�ġ ���� ��)
		ACT_DEPT_ORG_AVG_WAITING_TIME_HR AS ""WAIT AVG (ALL)"",	-- ��� ����LT ���(�̻�ġ ���� ��)
		ACT_MED_WAITING_TIME_HR AS ""WAIT MED"",			-- ��� ����LT MEDIAN
		ACT_DEPT_MIN_WAITING_TIME_HR AS ""WAIT MIN"",		-- ��� ����LT �ּҰ�
		ACT_DEPT_MAX_WAITING_TIME_HR AS ""WAIT MAX"",		-- ��� ����LT �ִ밪
		ACT_DEPT_STDEV_WAITING_TIME_HR AS ""WAIT DEV"",		-- ��� ����LT ǥ������
		FLOOR(LOT_CNT_DEPT_ALL) AS ""DATA COUNT"",
		FLOOR(LOT_CNT_PROCESS_REV) AS ""PROCESS DATA COUNT"",
		FLOOR(LOT_CNT_WAITING_REV) AS ""WAIT DATA COUNT"",
		A.USE_YN AS ""USE(Y/N)"", 
        A.INSERT_ID AS ""INSERT ID"", 
        FORMAT(A.INSERT_DTTM, 'yyyy-MM-dd HH:mm') AS ""INSERT DATE"", 
        A.UPDATE_ID AS ""UPDATE ID"", 
        FORMAT(A.UPDATE_DTTM, 'yyyy-MM-dd HH:mm') AS ""UPDATE DATE""				
from  TH_TAR_PLAN_DEPT_LEADTIME A
	  left outer join
	  TH_TAR_DEPT_MASTER B
	  ON A.DEPARTMENT_CODE = B.DEPARTMENT_CODE
where
    1=1
");
            /*
             * ������ ����
             */

            // Division
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
");
            } else
            {
                sSQL.Append($@"
    and A.DIVISION_ID in ('SPS','HDI')
");
            }

            // DEPARTMENT_CLASS
            if (terms["department_class"].Length > 0)
            {
                sSQL.Append($@"
    and (A.DEPARTMENT_CLASS_CODE like '%{terms["department_class"].AsString()}%' or A.DEPARTMENT_CLASS_NAME like '%{terms["department_class"].AsString()}%')
");
            }

            // DEPARTMENT
            if (terms["department"].Length > 0)
            {
                sSQL.Append($@"
    and (B.DEPARTMENT_NAME like '%{terms["department"].AsString()}%' or A.DEPARTMENT_CODE like '%{terms["department"].AsString()}%')
");
            }

            // USE_YN
            if (terms["use_yn"].Length > 0)
            {
                sSQL.Append($@"
    and A.USE_YN = '{terms["use_yn"].AsString()}'
");
            }

            // Empty Plan LT Only
            if (terms["empty_plan_only"].Length > 0)
            {
                if (terms["empty_plan_only"].AsString() == "Y")
                {
                    sSQL.Append($@"
    and (PLAN_PROCESSING_TIME_HR IS NULL and PLAN_ESSENTIAL_WAITING_TIME_HR IS NULL and PLAN_OPER_WAITING_TIME_HR IS NULL)
");
                }
                else
                {
                    sSQL.Append($@"
    and (PLAN_PROCESSING_TIME_HR IS NOT NULL and PLAN_ESSENTIAL_WAITING_TIME_HR IS NOT NULL and PLAN_OPER_WAITING_TIME_HR IS NOT NULL)
");
                }
            }

                sSQL.Append($@"
    order by A.DIVISION_ID ASC, A.DEPARTMENT_CLASS_CODE ASC, A.DEPARTMENT_CODE ASC
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            HS.Web.Proc.TH_TAR_PLAN_DEPT_LEADTIME.Save(data);
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


        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            if (command == "ExcelDownload")
            {
                //������ ��ȸ�� ������ ���� �ٿ�ε�
                DataTable dtResult = this.Search(e.Params["terms"]);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Resource_Master_" + timestamp);
            }
            else
                return Page();
        }
    }
}
