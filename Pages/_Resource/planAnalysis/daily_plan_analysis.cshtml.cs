using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Globalization;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class daily_plan_analysis : BasePageModel
    {

        public daily_plan_analysis()
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
select TOP 1
    PLAN_ID as PLAN_ID
from
    th_eng_plan_info with (NOLOCK)
            where
    master_id = 'SIMMTECH'
order by PLAN_START_DTTM desc, PLAN_ID asc
");

            Params result = Data.Get(sSQL.ToString()).Tables[0].ToParams();
            this.Params["first_plan_id"] = result["PLAN_ID"];
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
                //vali.Null("VIEW_YN", "보이기 여부가 입력되지 않았습니다.");

                vali.DoneDeco();

                this.Save(data);


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

            // ============================================================================
            // 시작날짜 ~ 종료날짜 PIVOT 조회하기 위해 [yyyy-MM-dd] 형식으로 만들기
            string startDateStr = terms["start_date"];
            string endDateStr = terms["end_date"];

            DateTime startDate = DateTime.ParseExact(startDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(endDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            List<string> dateList = new List<string>();
            
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateList.Add($"[{date:yyyy-MM-dd}]");
            }

            string result = string.Join(", ", dateList);
            // ============================================================================
            string yyyymmdd = terms["PLAN_ID"].AsString().Substring(4, 8); // 날짜 구해오기


            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
WITH 
PLAN_VERSION AS (
	SELECT 'SIMMTECH' AS MASTER_ID, {terms["PLAN_ID"].V}  AS PLAN_ID 	
),
APS_RESOURCE_MAP AS (	
	SELECT	A.MASTER_ID, A.PLAN_ID, A.RESOURCE_ID, A.RESOURCE_NAME, A.RESOURCE_GROUP_ID as RESOURCE_CAPA_GROUP_ID
	FROM	TH_MST_RESOURCE A
			JOIN 
			PLAN_VERSION B
			ON 1=1
	WHERE	A.MASTER_ID = B.MASTER_ID		-- 'SIMMTECH'		
	AND		A.PLAN_ID   = B.PLAN_ID			-- 'SIM_20250630_004' 
) -- select * from  APS_RESOURCE_MAP
,
--=============  Resource별 Daily Capa 
RES_CAPA AS (
	SELECT	A.PRODUCT_MIX_ID, MAX(A.QTY) AS RES_DAILY_CAPA
	FROM	TH_MST_PRODUCT_MIX A  --> 추후에 설비별 CAPA 고려해야 함. ATTRIBUTE에 RES MODELING 구분자 필요할 듯. 
			JOIN 
			PLAN_VERSION B
			ON 1=1 
	WHERE	A.MASTER_ID = B.MASTER_ID		-- 'SIMMTECH'		
	AND		A.PLAN_ID = B.PLAN_ID			-- 'SIM_20250630_004' 
	GROUP BY A.PRODUCT_MIX_ID
) -- SELECT * FROM RES_CAPA; 
,
--============= WIP 수량 산출용 시작 
WIP_LIST as (
	SELECT	-- A.YYYYMMDD, 
			ITEM_CODE, REVISION, JOB_ID, JOB_NAME, -- 참고용 
			WORKING_TYPE, -- HOLD 수량 별도로 집계. 값 목록 : HOLD / Waiting / Ongoing / Completed / Waiting to move / Moving   -- Move 관련 항목은 실제로는 없음.
			-- WORK_STATUS, ORGANIZATION_ID, 
			DEPT_CODE, -- OPERATION_SEQ_NUM, SCH_DATE, FIRST_UNIT_START_DATE, COMP_DATE, COMP_DATE2, DELTA, WAIT_TIME, 
			SQM, -- PRODUCT_WPNL, PRODUCT_PCS, PRODUCT_M2, SUBCONTRACTOR_FLAG, REPEAT, 
			INNER_OUTER, -- , MFG_CATEGORY, 
			PATTERN -- , INPUT_YIELD, INSERT_ID, INSERT_DTTM, UPDATE_ID, UPDATE_DTTM
	FROM	TH_TAR_WIP A
	where	1=1
    --yyyymmdd = '{yyyymmdd}'  -- UI WIP Cutoff Date 값을 문자열로 입력 #####
	and		ORGANIZATION_ID = 101 -- 현재는 101 단일값. 
), 
WIP_RCG_MAP_LIST as (
	select	A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, A.WORKING_TYPE, A.DEPT_CODE, A.SQM, A.INNER_OUTER, A.PATTERN,
			--B.APS_WIP_ROUTE_GRP_ID, B.WIP_ROUTE_GROUP_NAME, C.SORT_ORDER -- , B.sort_order 
			B.RESOURCE_CAPA_GROUP_ID, B.RESOURCE_CAPA_GROUP_NAME, C.SORT_ORDER -- , B.sort_order 
	from	WIP_LIST A
			inner join 
			TH_TAR_DEPT_MASTER_WITH_NAME_V B
			on A.DEPT_CODE = B.DEPT_CODE 
			inner join 
			RESOURCE_CAPA_GROUP_V C
			on B.RESOURCE_CAPA_GROUP_ID = C.RESOURCE_CAPA_GROUP_ID
) -- select * from WIP_RCG_MAP_LIST
,
RCG_WIP as (
	select  A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME,
			--case when A.WORKING_TYPE = 'HOLD' then 'Y' else 'N' end as HOLD_YN, 
			sum(A.SQM) as RCG_WIP_QTY 			
	from WIP_RCG_MAP_LIST A	
	group by A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME
) -- select * from RCG_WIP; 
--============= WIP 수량 산출용 종료 
,
WORK_ORDER_SPLIT AS (
	SELECT  A.RESOURCE_ID, A.ITEM_ID, A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, A.END_TIME, A.OPERATION_QTY, 
	        CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS BASE_DATE -- , SUBSTR(A.ROUTE_ID) AS DEPT_CODE 
	FROM	TH_OUT_WORK_ORDER_SPL	A WITH (NOLOCK)	
			JOIN 
			PLAN_VERSION B
			ON 1=1 
	WHERE	A.MASTER_ID = B.MASTER_ID		-- 'SIMMTECH'		
	AND		A.PLAN_ID = B.PLAN_ID			-- 'SIM_20250630_004' 
	AND		A.OUT_VERSION_ID = B.PLAN_ID	-- 'SIM_20250630_004'
), 
DAILY_PLAN_QTY_LIST AS (
	SELECT	A.RESOURCE_ID, B.RESOURCE_NAME, B.RESOURCE_CAPA_GROUP_ID, E.RESOURCE_CAPA_GROUP_NAME, D.RCG_WIP_QTY, C.RES_DAILY_CAPA,
			--CONVERT(NVARCHAR(10), A.BASE_DATE, 112) AS BASE_DATE, 
			CONVERT(NVARCHAR(10), A.BASE_DATE, 23) AS BASE_DATE, 
			SUM(A.OPERATION_QTY) AS DAILY_PLAN_QTY 
	FROM WORK_ORDER_SPLIT A
		LEFT OUTER JOIN 
		APS_RESOURCE_MAP B
		ON A.RESOURCE_ID = B.RESOURCE_ID 
		LEFT OUTER JOIN 
		RES_CAPA C
		ON A.RESOURCE_ID = C.PRODUCT_MIX_ID 
		LEFT OUTER JOIN
		RCG_WIP D 
		ON B.RESOURCE_CAPA_GROUP_ID = D.RESOURCE_CAPA_GROUP_ID
		LEFT OUTER JOIN 
		RESOURCE_CAPA_GROUP_V E
		on B.RESOURCE_CAPA_GROUP_ID = E.RESOURCE_CAPA_GROUP_ID
	where A.RESOURCE_ID not in ('V_I', 'V_O') 
	GROUP BY A.RESOURCE_ID, B.RESOURCE_NAME, B.RESOURCE_CAPA_GROUP_ID, E.RESOURCE_CAPA_GROUP_NAME, D.RCG_WIP_QTY, C.RES_DAILY_CAPA, A.BASE_DATE 
	--ORDER BY A.RESOURCE_ID, A.BASE_DATE 
) --SELECT * FROM DAILY_PLAN_QTY_LIST ;
SELECT * 
FROM ( 
	SELECT A.RESOURCE_ID, A.RESOURCE_NAME AS APS_RESOURCE_NAME, CEILING(A.RES_DAILY_CAPA) AS P_MIX_CAPA, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.RCG_WIP_QTY, A.BASE_DATE, CEILING(DAILY_PLAN_QTY) AS DAILY_PLAN_QTY
	FROM DAILY_PLAN_QTY_LIST A
	WHERE 1=1
");

            /*
             * 조건절 
             */
            if (terms["aps_resource_name"].Length > 0) // aps_resource_name 조건
            {
                sSQL.Append($@"
AND A.RESOURCE_NAME LIKE '%{terms["aps_resource_name"].AsString()}%'
");
            }

            sSQL.Append($@"
) A
PIVOT ( SUM(DAILY_PLAN_QTY) --  실제로는 SUM 아님 
		FOR BASE_DATE IN ({result}) 
	  ) AS PIVOT_TABLE
ORDER BY RESOURCE_CAPA_GROUP_ID, RESOURCE_ID 
");

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
        private void Save(Params data)
        {
            //HS.Web.Proc.SAF_WRK_CLS.Save(data);
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

        /// <summary>
        /// 저장된 그리드 헤더컬럼 가져오기
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
        /// 그리드 헤더컬럼 옵션 저장
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
                //데이터 조회한 값으로 엑셀 다운로드
                DataTable dtResult = this.Search(e.Params["terms"]);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Lot_Routing_Sequence_" + timestamp);
            }
            else
                return Page();
        }
    }
}
