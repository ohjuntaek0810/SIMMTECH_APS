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
    public class daily_plan_analysis_test : BasePageModel
    {

        public daily_plan_analysis_test()
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;

            Params result = HS.Web.Common.ApsManage.searchPlanId().ToParams();
            Console.WriteLine(result["PLAN_ID"].AsString());
            this.Params["first_plan_id"] = result["PLAN_ID"];
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                Vali vali = new Vali(terms);
                vali.Null("item_code", "ITEM_CODE를 입력해주세요.");

                vali.DoneDeco();

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

            // 날짜 리스트 기반으로 "날짜 IS NOT NULL OR" 조건 만들기
            List<string> whereConditions = new List<string>();

            foreach (string date in dateList)
            {
                whereConditions.Add($"{date} IS NOT NULL");
            }

            string whereClause = string.Join(" OR\n\t", whereConditions);
            // ============================================================================
            string yyyymmdd = terms["PLAN_ID"].AsString().Substring(4, 8); // 날짜 구해오기


            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
WITH 
-- WIP_ROUTE_GROUP_LIST --> WIP_ROUTE_GROUP_V 사용 
PLAN_VERSION AS (
	SELECT 'SIMMTECH' AS MASTER_ID, {terms["PLAN_ID"].V} AS PLAN_ID 	-- 임시 
	--SELECT MASTER_ID, MAX(PLAN_ID) AS PLAN_ID  
	--FROM TH_ENG_PLAN_INFO  WITH (NOLOCK)
	--WHERE MASTER_ID = 'SIMMTECH'
	--GROUP BY  MASTER_ID
),
PST AS (
	SELECT PLAN_START_DTTM, PLAN_START_DTTM + PLAN_HORIZON AS PLAN_END_DTTM 
	FROM TH_ENG_PLAN_INFO  WITH (NOLOCK)
	WHERE MASTER_ID = 'SIMMTECH' AND PLAN_ID = {terms["PLAN_ID"].V} 
), 
PLAN_RESULT_ORG AS (  -- ROUTE를 원래의 DEPT로 표시하고 ITEM CODE도 원본으로 변경함 
	SELECT  -- MASTER_ID, PLAN_ID, OUT_VERSION_ID, WORK_ORDER_ID, 
			--#### 7/9 JOB NAME C.IRS_ATTB_9 사용하는걸로 수정할 것  
			SUBSTRING(A.ITEM_ID, CHARINDEX('_',  A.ITEM_ID)+1, CASE WHEN CHARINDEX('__',  A.ITEM_ID) > 0 THEN CHARINDEX('__',  A.ITEM_ID) - CHARINDEX('_',  A.ITEM_ID) -1 ELSE LEN(A.ITEM_ID) -  CHARINDEX('_',  A.ITEM_ID) END ) AS JOB_NAME,  
			--C.IRS_ATTB_9 AS JOB_NAME, -- PLAN ID에 따라 안 되어있는 것도 있음. 
			CASE WHEN LEFT(D.D_CATEGORY, 1) = 'S' THEN 'SPS' WHEN LEFT(D.D_CATEGORY, 1) = 'H' THEN 'HDI' END AS DIVISION_ID, 
			C.IRS_ATTB_2 AS ITEM_CODE, C.IRS_ATTB_3 AS REVISION, C.IRS_ATTB_4 AS ST_SITE, C.IRS_ATTB_5 AS DEPT_CODE, C.IRS_ATTB_6 AS DEPT_NAME, 
			C.IRS_ATTB_12 AS LAYER_IN_OUT, -- ###  추후 IRS_ATTB_10으로 들어올 것
			ROUTE_LEVEL AS OPER_SEQ, -- SEQ 맞는지? 
			A.RESOURCE_ID, 
			-- UI용 작업/대기시간 
			CONVERT(DATE, DATEADD(MINUTE, -450, A.START_TIME)) AS BASE_DATE,  --### 공정 시작시간 기준으로 BASE DATE 정함. 설비부하분석 기준은 END_DATE인데, START 기준으로 통일하자. 
			--DATEADD(SS, - C.BUFFER_TIME*60.0*60.0 - C.IN_MOVE_TIME*60.0*60.0, A.START_TIME ) AS CLOCK_ACCEPT_TIME,  -- 대기시간 역산으로 계산. WIP 
			--A.START_TIME AS PROCESS_START_TIME, 
			--DATEADD(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS PROCESS_END_TIME,
			--DATEADD(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS CLOCK_OUT_TIME,			
			A.OPERATION_QTY --, COMPLETE_QTY, BATCH_QTY
			--JOB_NAME, -- 가져올 곳이 없음. MST_ITEM_ROUTE_SITE에 JOB_NAME ATTRIBUTE 추가할 것.  IRS_ATTB_9로 추가함.
	FROM	TH_OUT_WORK_ORDER A	WITH (NOLOCK)
			INNER JOIN 
			PLAN_VERSION B WITH (NOLOCK)
			ON A.MASTER_ID = B.MASTER_ID
			AND A.PLAN_ID = B.PLAN_ID
			AND A.OUT_VERSION_ID  = B.PLAN_ID
			INNER JOIN -- 임시 
			TH_MST_ITEM_ROUTE_SITE C WITH (NOLOCK)
			ON A.MASTER_ID = C.MASTER_ID
            AND C.PLANT = 'STK'
            AND C.CORPORATION = 'STK'
			AND A.PLAN_ID =  C.PLAN_ID 
			AND A.SITE_ID = C.SITE_ID
			AND A.ITEM_ID = C.ITEM_ID
			AND A.ROUTE_ID = C.ROUTE_ID 	
			LEFT OUTER JOIN
			CBST_SPEC_BASIC D WITH (NOLOCK)
			ON C.IRS_ATTB_2 = D.ITEM_CODE
			AND  C.IRS_ATTB_3 = D.REVISION
	WHERE	1=1
	--AND		A.MASTER_ID = 'SIMMTECH' 
	--AND		A.PLAN_ID = 'SIM_20250630_004' 
	--AND		A.OUT_VERSION_ID = 'SIM_20250630_004' 
	AND		A.RESOURCE_ID NOT IN ('V_I', 'V_O') 
--	AND		C.IRS_ATTB_2 = 'MCP22030A00'		--	ITEM_CODE  조건 필수 
");

            // ITEM_CODE 조건 추가
            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
AND		C.IRS_ATTB_2 LIKE '%{terms["item_code"].AsString()}%'		--	ITEM_CODE  조건 필수 
");
            }

            sSQL.Append($@"
) 
--SELECT * FROM PLAN_RESULT_ORG A ORDER BY A.PROCESS_START_TIME, JOB_NAME, OPER_SEQ, A.RESOURCE_ID ;
, 
PLAN_RESULT_WRG AS (
	SELECT	A.*, B.APS_WIP_ROUTE_GRP_ID, B.WIP_ROUTE_GROUP_NAME 
	FROM	PLAN_RESULT_ORG A WITH (NOLOCK)
			LEFT OUTER JOIN
			TH_TAR_DEPT_MASTER_WITH_NAME_V B WITH (NOLOCK)
			ON A.DEPT_CODE = B.DEPT_CODE
)
--SELECT * FROM  PLAN_RESULT_WRG A ORDER BY A.ITEM_CODE, A.JOB_NAME, A.OPER_SEQ, A.RESOURCE_ID, A.PROCESS_START_TIME;
-- 내외층 WRG 별로 집계 
, 
WRG_DATE_QTY_LIST AS (
	SELECT	A.DIVISION_ID, A.ITEM_CODE, A.LAYER_IN_OUT, A.APS_WIP_ROUTE_GRP_ID, A.BASE_DATE, 
			SUM(A.OPERATION_QTY) AS WRG_DATE_QTY
	FROM	PLAN_RESULT_WRG A WITH (NOLOCK)
	GROUP BY A.DIVISION_ID, A.ITEM_CODE, A.LAYER_IN_OUT, A.APS_WIP_ROUTE_GRP_ID, A.BASE_DATE
	-- ORDER BY A.ITEM_CODE, A.JOB_NAME,  A.LAYER_IN_OUT, A.APS_WIP_ROUTE_GRP_ID
) 
--SELECT * FROM WRG_DATE_QTY_LIST A ORDER BY A.ITEM_CODE, A.JOB_NAME, A.LAYER_IN_OUT, A.APS_WIP_ROUTE_GRP_ID, A.BASE_DATE
,
ITEM_LIST AS (
	SELECT DISTINCT A.DIVISION_ID, A.ITEM_CODE, A.LAYER_IN_OUT FROM WRG_DATE_QTY_LIST A
),
DATE_LIST AS (
	--SELECT THEDATE FROM [DBO].[TEMP_CALENDAR] WHERE THEDATE BETWEEN '2025-07-01' AND  '2025-07-07' 
	SELECT THEDATE 
	FROM [DBO].[TEMP_CALENDAR] A WITH (NOLOCK)
			CROSS JOIN 
			PST B
	WHERE THEDATE BETWEEN PLAN_START_DTTM - 1 AND PLAN_START_DTTM + 14 --  PLAN_END_DTTM
) 
--SELECT * FROM DATE_LIST; 
,
ITEM_WRG_LAYER_INOUT_DATE_FULL_LIST AS (
        SELECT  A.DIVISION_ID, D.LAYER_IN_OUT, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.SORT_ORDER,
                    C.THEDATE, D.ITEM_CODE
        FROM    
        WIP_ROUTE_GROUP_INOUT_V A -- select * from WIP_ROUTE_GROUP_INOUT_V
        CROSS JOIN
        DATE_LIST C
        INNER JOIN
        ITEM_LIST D
        ON A.LAYER_INOUT = D.LAYER_IN_OUT
        WHERE A.DIVISION_ID = D.DIVISION_ID
)
--SELECT * FROM ITEM_WRG_LAYER_INOUT_DATE_FULL_LIST ORDER BY DIVISION_ID, ITEM_CODE, LAYER_IN_OUT, SORT_ORDER; 
, 
ITEM_WRG_LAYER_INOUT_FULL_LIST2 AS (
	SELECT	A.DIVISION_ID, A.ITEM_CODE, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.SORT_ORDER, A.LAYER_IN_OUT , A.THEDATE, B.WRG_DATE_QTY  -- B.JOB_NAME, 
	FROM	ITEM_WRG_LAYER_INOUT_DATE_FULL_LIST A
			LEFT OUTER JOIN
			WRG_DATE_QTY_LIST B 
			ON A.APS_WIP_ROUTE_GRP_ID = B.APS_WIP_ROUTE_GRP_ID
			AND A.LAYER_IN_OUT = B.LAYER_IN_OUT
			AND A.ITEM_CODE = B.ITEM_CODE
			AND A.THEDATE = B.BASE_DATE
	--ORDER BY A.LAYER_IN_OUT, A.SORT_ORDER, B.BASE_DATE
) 
--SELECT * FROM ITEM_WRG_LAYER_INOUT_FULL_LIST2 A ORDER BY A.DIVISION_ID, A.ITEM_CODE, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.SORT_ORDER, A.LAYER_IN_OUT , A.THEDATE;

SELECT	* 
FROM	( 
			SELECT
	            A.DIVISION_ID AS ""GROUP"",
                A.ITEM_CODE AS ""ITEM CODE"",
                B.CUSTOMER,
                A.APS_WIP_ROUTE_GRP_ID,
                A.WIP_ROUTE_GROUP_NAME AS ""WIP ROUTE GROUP"",
                A.SORT_ORDER,
                A.LAYER_IN_OUT AS ""IN/OUT"",
                A.THEDATE,
                A.WRG_DATE_QTY
            FROM ITEM_WRG_LAYER_INOUT_FULL_LIST2 A
                left join TH_GUI_ITEM_MODEL_SEARCH B
                ON A.ITEM_CODE = B.ITEM_CODE
            WHERE 1=1 
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
                AND A.DIVISION_ID = {terms["group_id"].V}
");
            }
            if (terms["customer"].Length > 0)
            {
                sSQL.Append($@"
                AND B.CUSTOMER LIKE '%{terms["customer"].AsString()}%'
");
            }


            sSQL.Append($@"
		) A
PIVOT (  
		SUM(WRG_DATE_QTY)
		FOR THEDATE IN ({result}) 
	  ) AS PIVOT_TABLE
WHERE 1=1 AND
        {whereClause}
ORDER BY ""ITEM CODE"", ""IN/OUT"", SORT_ORDER
");
            Console.WriteLine(sSQL.ToString());

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
