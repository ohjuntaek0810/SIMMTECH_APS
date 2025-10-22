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
    public class resource_master : BasePageModel
    {
        private readonly ILogger<resource_master> _logger;

        public resource_master(ILogger<resource_master> logger)
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
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                this.Save(dataList);


                // 데이터 저장
            }

            else if(e.Command == "delete")
            {
                ParamList data = e.Params["data"];


                this.delete(data);
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

            sSQL.Append(@"
select  A.DIVISION_ID AS ""GROUP"",
        B.DEPARTMENT_CLASS_CODE as ""CLASS CODE"", 
        B.DEPARTMENT_CLASS_NAME as ""CLASS NAME"", 
        A.DEPT_CODE AS ""DEPARTMENT CODE"", 
		B.DEPARTMENT_NAME  as ""DEPARTMENT NAME"", 
		C.DEPT_GROUP_NAME as ""DEPARTMENT GROUP"",  
		C.RESOURCE_GROUP AS ""RESOURCE GROUP"", 
        A.RESOURCE_CAPA_GROUP_ID AS ""RESOURCE CAPA ID"", 
        D.RESOURCE_CAPA_GROUP_NAME AS ""RESOURCE CAPA NAME"",
		A.RESOURCE_ID AS ""RESOURCE ID"",
		A.RESOURCE_CODE AS ""RESOURCE CODE"", 
        A.RESOURCE_NAME AS ""RESOURCE NAME"",
        C.FACTORY_FLOOR AS ""SITE"", 
        A.USE_YN as ""APS USE(Y/N)"", --###  수정 가능
        A.USE_TYPE AS ""MES USE(Y/N)"", -- 컬럼명 ""USE"",
        C.APPLY_OPTION AS ""APPLY"", 
        A.OUTSOURCE_YN AS ""OUTSOURCING"",  -- 컬럼명 OUTSOURCING  수정 가능 
        A.ecim_skip_validate as ECIM, 
		A.VIRTUAL_TYPE as VIRTUAL, 
		A.MAIN_TYPE as MAIN, 
		C.PORTION_TACT_TIME AS ""TACT TIME"",
		CEILING(C.M2_MONTH_PROD) AS ""CAPA/MONTH"", -- 컬럼명 ""CAPA/MONTH""  
		floor(C.M2_MONTH_PROD/29) as ""CAPA/DAY"", -- 컬럼명 ""CAPA/DAY""
		C.OPERATION_RATE AS ""OPERATION RATE"", 
		C.DOWN_TIME AS ""DOWN TIME"", 
        C.THICKNESS_MIN AS ""THICKNESS MIN"", 
        C.THICKNESS_MAX AS ""THICKNESS MAX"", 
        C.XWPNL_MIN AS ""XWPNL MIN"", 
        C.XWPNL_MAX AS ""XWPNL MAX"", 
        C.YWPNL_MIN AS ""YWPNL MIN"", 
        C.YWPNL_MAX AS ""YWPNL MAX"",
        C.MAKER, 
        FORMAT(C.REV_DATE, 'yyyy-MM-dd HH:mm') AS ""REV DATE"", 
		C.DEPT_GROUP_ID,         
		B.SITE_ID as ""APS SITE"", 
		--C.UP_DATE, 
        A.PATTERN_TYPE AS ""PATTERN TYPE"",
		A.UPDATE_ID AS ""UPDATE ID"",   --""변경자"" 
		FORMAT(A.UPDATE_DTTM, 'yyyy-MM-dd HH:mm') AS ""UPDATE DATE"" --""변경일"" 		
from	TH_TAR_RESOURCE_MASTER A  -- 원본1 
		left outer join 
		TH_TAR_DEPT_MASTER B 
		on A.DEPT_CODE = B.DEPARTMENT_CODE
		left outer join 
		TH_TAR_MES_CAPA_L C		-- 원본2. 최신 버전만.
		on A.RESOURCE_CODE = C.RESOURCE_CODE AND C.CAPA_REV in ( select CAPA_REV from TH_TAR_MES_CAPA_H where USE_YN = 'Y' )  --SPS, HDI 최신버전 
		left outer join 
		RESOURCE_CAPA_GROUP_V D
		on A.RESOURCE_CAPA_GROUP_ID = D.RESOURCE_CAPA_GROUP_ID
where 
    1=1
");
            /*
             * 조건절 시작
             */
            if (terms["group_id"].Length > 0 ) // 사업부(그룹) 조건
            {
                sSQL.Append($@"
AND A.DIVISION_ID = {terms["group_id"].V}
");
            }

            if (terms["resource_code"].Length > 0) // resource_code 조건
            {
                sSQL.Append($@"
AND A.RESOURCE_CODE = {terms["resource_code"].V}
");
            }

            if (terms["resource_name"].Length > 0) // resource_name 조건
            {
                sSQL.Append($@"
AND A.RESOURCE_NAME = {terms["resource_name"].V}
");
            }

            if (terms["dept_class_name"].Length > 0) // dept_class_name 조건
            {
                sSQL.Append($@"
AND B.DEPARTMENT_CLASS_CODE = {terms["dept_class_name"].V}
");
            }

            if (terms["dept_name"].Length > 0) // dept_name 조건
            {
                sSQL.Append($@"
AND A.DEPT_CODE = {terms["dept_name"].V}
");
            }



            if (terms["app_use"].Length > 0) // APP_USE 조건
            {
                sSQL.Append($@"
AND A.USE_YN = {terms["app_use"].V}
"); 
            }

            if (terms["outsourcing"].Length > 0) // OUTSOURCE_YN 조건
            {
                sSQL.Append($@"
AND A.OUTSOURCE_YN = {terms["outsourcing"].V}
");
            }

            if (terms["mes_use"].Length > 0) // OUTSOURCE_YN 조건
            {
                sSQL.Append($@"
AND A.USE_TYPE = {terms["mes_use"].V}
");
            }

            if (terms["site"].Length > 0) // site 조건
            {
                sSQL.Append($@"
AND B.SITE_ID = {terms["site"].V}
");
            }



            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private Params search_chart(Params terms)
        {
            Params result = new();

            return result;
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            HS.Web.Proc.TH_TAR_RESOURCE_MASTER.Save(data);
        }

        /// <summary>
        /// 선택한 항목 삭제
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            throw new Exception("준비중입니다.");

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
                //데이터 조회한 값으로 엑셀 다운로드
                DataTable dtResult = this.Search(e.Params["terms"]);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Resource_Master_" + timestamp);
            }
            else
                return Page();
        }
    }
}
