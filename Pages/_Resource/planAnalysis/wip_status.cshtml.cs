using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Data;
using System.Reactive.Joins;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class wip_status : BasePageModel
    {

        public wip_status()
        {
            this.Handler = handler;

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
                ParamList headers = e.Params["headers"];

                toClient["data"] = this.Search(terms, headers);
				toClient["data_expectation_target"] = this.SearchExpectationTarget(terms, headers);
                toClient["data_production_planned_quantity"] = this.SearchProductionPlanedQuantity(terms, headers);
                toClient["data_month_available_quantity"] = this.SearchMonthAvailableQuantity(terms, headers);
                toClient["search_summary_actual"] = this.search_summary_actual(terms);
            }

            else if (e.Command == "search_header" )
			{
                Params terms = e.Params["terms"];

                Vali vali = new Vali(terms);
                vali.Null("group_id", "GROUP을 선택해 주세요.");

                vali.DoneDeco();

                toClient["data"] = this.search_header(terms);
            }


			else if (e.Command == "view")
			{
				Params terms = e.Params["terms"];

				//toClient["data"] = this.Search(terms);
			}

			else if (e.Command == "save")
			{
				ParamList dataList = e.Params["data"];

				// 데이터 저장
				this.Save(dataList);
			}

			else if (e.Command == "delete")
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
        private DataTable Search(Params terms, ParamList headers)
        {

            DTClient.UserInfoMerge(terms);

			// pivot 값 넣기 위한 변수
			List<string> dataFieldList = new List<string>();
			// sum pivot 값 넣기 위한 변수
            List<string> dataFieldList2 = new List<string>();
            // select 조회를 위한 변수
            List<string> dataFieldList3 = new List<string>();
			// TOTAL 계산용
            List<string> dataFieldList4 = new List<string>();

            foreach (var item in headers)
            {
                dataFieldList.Add($"\"{item["label"].AsString()}\"");
                dataFieldList2.Add($"SUM(\"{item["label"].AsString()}\")");

				// "[IN] MLB" AS IN_MLB    이런형태가되어야함
				// "label" AS dataField
				dataFieldList3.Add($"ROUND(\"{item["label"].AsString()}\", 0) AS \"{item["dataField"].AsString()}\"");

                // TOTAL 계산용: ISNULL("label", 0)
                dataFieldList4.Add($"ISNULL(\"{item["label"].AsString()}\", 0)");
            }
			string result = string.Join(", ", dataFieldList);
            string result2 = string.Join(", ", dataFieldList2);
            string result3 = string.Join(", ", dataFieldList3);

            // TOTAL 열 계산식
            string totalExpression = string.Join(" + ", dataFieldList4);
            string totalColumn = $"ROUND({totalExpression}, 0) AS TOTAL";


            string today = DateTime.Now.ToString("yyyyMMdd");
            string yyyymmdd = terms["PLAN_ID"].AsString().Substring(5, 8); // 날짜 구해오기

            StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
-- WIP + In/Out + WIP_ROUTE_GROUP
with 
plan_info as (
	--select '20250519' as plan_start_date 
	select master_id, plan_id, convert(varchar(8), plan_start_dttm, 112) as plan_start_date  -- 20250630
	from th_eng_plan_info 
	where master_id = 'SIMMTECH' and plan_id = {terms["PLAN_ID"].V}
), 
cufoff_Date as (
select plan_id, PLAN_ATTB_1 as WIP_YYYYMMDD,  PLAN_ATTB_2 as WIP_SEQ
from  th_mst_plan with (nolock)
WHERE PLAN_ID = {terms["PLAN_ID"].V}
),
WIP_LIST as (
        SELECT  -- A.YYYYMMDD,
                        ITEM_CODE, REVISION, JOB_ID, JOB_NAME, -- 참고용
                        WORKING_TYPE, -- HOLD 수량 별도로 집계. 값 목록 : HOLD / Waiting / Ongoing / Completed / Waiting to move / Moving   -- Move 관련 항목은 실제로는 없음.
                        -- WORK_STATUS, ORGANIZATION_ID,
                        DEPT_CODE, -- OPERATION_SEQ_NUM, SCH_DATE, FIRST_UNIT_START_DATE, COMP_DATE, COMP_DATE2, DELTA, WAIT_TIME,
                        SQM, -- PRODUCT_WPNL, PRODUCT_PCS, PRODUCT_M2, SUBCONTRACTOR_FLAG, REPEAT,
                        CASE WHEN INNER_OUTER = 'I' THEN 'INNER'
                                        ELSE 'OUTER' END AS INNER_OUTER,
                        PATTERN -- , INPUT_YIELD, INSERT_ID, INSERT_DTTM, UPDATE_ID, UPDATE_DTTM
        FROM    TH_TAR_WIP_HIS A WITH (NOLOCK)
        where
	        1=1
	    	and ORGANIZATION_ID = 101 -- 현재는 101 단일값.
	    	and yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
			and SEQ = (select WIP_SEQ from cufoff_Date)
			and USE_YN = 'Y'
");

			if (terms["order_type"].Length > 0)
			{
				if (terms["order_type"].AsString() == "MASS") // 양산 M 
				{
					sSQL.Append($@"
    AND LEFT(JOB_NAME, 1) = 'M'
");
				}
				else if (terms["order_type"].AsString() == "SAMPLE") // SAMPLE NOT T, NOT M 
				{
					sSQL.Append($@"
    AND LEFT(JOB_NAME, 1) != 'T' 
    AND LEFT(JOB_NAME, 1) != 'M'
");
				}
				else if (terms["order_type"].AsString() == "TEST") // TEST T 
				{
					sSQL.Append($@"
    AND LEFT(JOB_NAME, 1) = 'T'
");
				}
			}



			sSQL.Append($@"
), 
inout_group_mapping_raw as (
	select	B.DIVISION_ID, A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, A.WORKING_TYPE, A.DEPT_CODE, A.SQM, A.INNER_OUTER, A.PATTERN,
			B.APS_WIP_ROUTE_GRP_ID, B.WIP_ROUTE_GROUP_NAME 
	from	WIP_LIST A WITH (NOLOCK)
			inner join 
			TH_TAR_DEPT_MASTER_WITH_NAME_V B WITH (NOLOCK)
			on A.DEPT_CODE = B.dept_code 
--		WHERE C.APS_WIP_ROUTE_GRP_ID = 'APS_WRG_0001' -- TODO : 여기다가 걸어서 다른거는 헤더에 안들어가게 조절해야겠네
) --select * from inout_group_mapping_raw;
,
rst1 as (
	select  -- A.INNER_OUTER, 
			A.DIVISION_ID, A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME,
			A.INNER_OUTER, 
			A.PATTERN,
			A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME,
			--case when A.INNER_OUTER ='I' then 'IN  ' when A.INNER_OUTER ='O' then 'OUT  ' end + A.WIP_ROUTE_GROUP_NAME as column_name, 
			-- A.WORKING_TYPE, -- 개별 분류 안함 
			case when A.WORKING_TYPE = 'HOLD' then 'Y' else 'N' end as HOLD_YN, 
			A.SQM			
	from inout_group_mapping_raw A	
) --select * from rst1;
,
rst2 as (
	select	A.DIVISION_ID , A.INNER_OUTER,  A.PATTERN, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.HOLD_YN, 
			sum(A.SQM) as TOT_SQM, count(*) as LOT_CNT
	from rst1  A
	group by  A.DIVISION_ID ,A.INNER_OUTER, A.PATTERN, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.HOLD_YN  
) --select * from rst2;
,
rst3 as (
	select	A.DIVISION_ID , B.HOLD_YN, A.WRG_IO_NAME,
			C.CATEGORY_LEVEL1, B.PATTERN, B.TOT_SQM, 
			A.SORT_ORDER, A.LAYER_INOUT
	from 
		WIP_ROUTE_GROUP_INOUT_V A  WITH (NOLOCK)-- 외층만 있는경우 내층만있는 경우가 반영됨
	left join rst2 B
		on A.APS_WIP_ROUTE_GRP_ID = B.APS_WIP_ROUTE_GRP_ID
		AND A.LAYER_INOUT = B.INNER_OUTER
		AND A.DIVISION_ID = B.DIVISION_ID
	left join
	(select 
	SEGMENT2 AS GROUP_ID,
	SEGMENT3 AS CATEGORY_LEVEL1,
	SEGMENT4 AS CATEGORY_LEVEL2
from 
	[dbo].[LOOKUP_VALUE_M]
WHERE 
	1=1
	AND LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY'
	AND LOOKUP_TYPE_VERSION = ( select MAX(LOOKUP_TYPE_VERSION)
								FROM [dbo].[LOOKUP_VALUE_M]
								WHERE LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY')) C
								ON A.DIVISION_ID = C.GROUP_ID
								AND B.PATTERN = C.CATEGORY_LEVEL2
	where 1=1
		  AND HOLD_YN IS NOT NULL
");

			if (terms["hold_yn"].AsBool())
			{

			} else
			{
				sSQL.Append($@"
		and HOLD_YN = 'N' -- 조회 조건에 따라 설정 
");
			}

			// DIVISION_ID
			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
	AND A.DIVISION_ID = {terms["group_id"].V}
");
			}
			// 대분류
			if (terms["category_level1"].Length > 0)
			{
				sSQL.Append($@"
	AND C.CATEGORY_LEVEL1 = {terms["category_level1"].V}
");
			}
            // 상세
            if (terms["category_level2"].Length > 0)
            {
                sSQL.Append($@"
	AND B.PATTERN = {terms["category_level2"].V}
");
            }
            // 내외층
            if (terms["in_out"].Length > 0)
            {
				if (terms["in_out"].AsString() == "IN")
				{
                    sSQL.Append($@"
	AND A.LAYER_INOUT = 'INNER'
");
                } else
				{
                    sSQL.Append($@"
	AND A.LAYER_INOUT = 'OUTER'
");
                }
            }
            // WIP Route Grp
            if (terms["aps_wip_route_group_name"].Length > 0)
            {
				// terms["aps_wip_route_group_name"] 데이터 짤라서 사용해야함
				string[] wip_list = terms["aps_wip_route_group_name"].AsString().Split(",");
				List<string> wip_result_list = new List<string>();

                foreach (var item in wip_list)
                {
					wip_result_list.Add($@"'{item}'");
                }

				string wip_result = string.Join(',', wip_result_list);



                sSQL.Append($@"
	and A.APS_WIP_ROUTE_GRP_ID in ({wip_result})
");
            }



            sSQL.Append($@"
) --select * from rst3;
,
rst_pivot_base AS (
	SELECT * 
	FROM (
		SELECT 'WIP 분포' AS MAIN_CATEGORY, CATEGORY_LEVEL1, PATTERN, WRG_IO_NAME, TOT_SQM AS TOT_SQM
		FROM rst3
	) result
	PIVOT (
		SUM(TOT_SQM) FOR WRG_IO_NAME IN (
			{result}
		)
	) AS PIVOT_RESULT
),
rst_pivot_total as
-- 최종 출력: 피벗된 상세 + MSAP_SUM + TOTAL
(SELECT * FROM rst_pivot_base
UNION ALL
-- MSAP_SUM 행
SELECT 
	'WIP 분포' AS MAIN_CATEGORY, 'MSAP', 'SUM',
	{result2}
FROM rst_pivot_base
WHERE CATEGORY_LEVEL1 = 'MSAP'
UNION ALL
-- TOTAL 행
SELECT 
	'WIP 분포' AS MAIN_CATEGORY, 'TOTAL', 'TOTAL',
	{result2}
FROM rst_pivot_base
) 
select
	MAIN_CATEGORY,
	CATEGORY_LEVEL1,
	PATTERN,
	{totalColumn},
	{result3}
from 
	rst_pivot_total
order by CATEGORY_LEVEL1
");

			//Console.WriteLine(sSQL.ToString());

			return Data.Get(sSQL.ToString()).Tables[0];
        }

		private DataTable search_header(Params terms)
		{
            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
select 
	WRG_IO_NAME AS NAME,
	REPLACE(REPLACE(REPLACE(WRG_IO_NAME, ']', ''), '[', ''), ' ', '_') AS CODE
from 
	WIP_ROUTE_GROUP_INOUT_V 
where 
	1=1
");
			/**
			 *  조건절
			*/
			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
	and DIVISION_ID = {terms["group_id"].V}
");
			}
            // 내외층
            if (terms["in_out"].Length > 0)
            {
                if (terms["in_out"].AsString() == "IN")
                {
                    sSQL.Append($@"
	AND LAYER_INOUT = 'INNER'
");
                }
                else
                {
                    sSQL.Append($@"
	AND LAYER_INOUT = 'OUTER'
");
                }
            }
            // WIP Route Grp
            if (terms["aps_wip_route_group_name"].Length > 0)
            {
                // TODO : terms["aps_wip_route_group_name"] 데이터 짤라서 사용해야함
                string[] wip_list = terms["aps_wip_route_group_name"].AsString().Split(",");
                List<string> wip_result_list = new List<string>();

                foreach (var item in wip_list)
                {
                    wip_result_list.Add($@"'{item}'");
                }

                string wip_result = string.Join(',', wip_result_list);



                sSQL.Append($@"
	and APS_WIP_ROUTE_GRP_ID IN ({wip_result})
");
            }

            sSQL.Append($@"
order by LAYER_INOUT, SORT_ORDER
");

			//Console.WriteLine(sSQL.ToString());

			return Data.Get(sSQL.ToString()).Tables[0];
		}


		// 생산계획 물량(APS) Data 
		private DataTable SearchProductionPlanedQuantity(Params terms, ParamList headers)
		{

            DTClient.UserInfoMerge(terms);

            // pivot 값 넣기 위한 변수
			List<string> dataFieldList = new List<string>();
			// sum pivot 값 넣기 위한 변수
            List<string> dataFieldList2 = new List<string>();
            // select 조회를 위한 변수
            List<string> dataFieldList3 = new List<string>();
			// TOTAL 계산용
            List<string> dataFieldList4 = new List<string>();
            // SUM TOTAL 계산용
            List<string> dataFieldList5 = new List<string>();

            foreach (var item in headers)
            {
                dataFieldList.Add($"\"{item["label"].AsString()}\"");
                dataFieldList2.Add($"SUM(\"{item["label"].AsString()}\")");

				// "[IN] MLB" AS IN_MLB    이런형태가되어야함
				// "label" AS dataField
				dataFieldList3.Add($"\"{item["label"].AsString()}\" AS \"{item["dataField"].AsString()}\"");
                // TOTAL 계산용: ISNULL("label", 0)
                dataFieldList4.Add($"ISNULL(\"{item["label"].AsString()}\", 0)");
                // SUM TOTAL 계산용: ISNULL(SUM("label"), 0)
                dataFieldList5.Add($"ISNULL(SUM(\"{item["label"].AsString()}\"), 0)");

            }
			string result = string.Join(", ", dataFieldList);
            string result2 = string.Join(", ", dataFieldList2);
            string result3 = string.Join(", ", dataFieldList3);

            // TOTAL 열 계산식
            string totalExpression = string.Join(" + ", dataFieldList4);
            string totalColumn = $"{totalExpression} AS TOTAL";

            // SUM TOTAL 열 계산식
            string sumTotalExpression = string.Join(" + ", dataFieldList5);
            string sumTotalColumn = $"{sumTotalExpression} AS TOTAL";

			StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
with 
PST as (
	select master_id, plan_id, PLAN_START_DTTM, PLAN_START_DTTM+1 as TO_DTTM
	from th_eng_plan_info with (NOLOCK) 
	where master_id = 'SIMMTECH' 
	and plan_id = {terms["PLAN_ID"].V}  -- UI에서 생산계획 버전 선택 필요 ###########
	--order by plan_id desc
),
WIP_WRG_IO as (
	select	--A.DIVISION_ID, A.ITEM_CODE, A.REVISION, 
			A.master_id, A.plan_id, 
			DMD_ATTB_8 as DIVISION_ID,
			A.DEMAND_ID as JOB_NAME, A.SALES_MODEL as ORG_ITEM_CODE, -- , D.APS_WIP_ROUTE_GRP_ID, D.WIP_ROUTE_GROUP_NAME, D.LAYER_INOUT, D.WRG_IO_ID, D.WRG_IO_NAME, A.PATTERN
			A.DMD_ATTB_6 as APS_WIP_ROUTE_GRP_ID, A.DMD_ATTB_13 as WIP_ROUTING_LAYER_INOUT,
			C.WRG_IO_ID, C.WRG_IO_NAME,
			B.PLAN_START_DTTM, B.TO_DTTM,
			A.DMD_ATTB_16 as FIRST_OPER_SEQ -- TH_MST_DEMAND에서 현재 WIP OPER_SEQ, 단, COMPLETED 이면 다음 공정의 SEQ. Join 용. 
	from	--TH_TAR_WIP_HIS A  with (nolock)  -- select top 100 * from  TH_TAR_WIP_HIS with (nolock)
			TH_MST_DEMAND A with (NOLOCK) -- select * from TH_MST_DEMAND  with (nolock) where CORPORATION = 'STK' and plant = 'STK' and master_id = 'SIMMTECH'  and plan_id = 'SIMM_20250917_P01' 
			inner join 
			PST B 
			on  A.master_id = B.master_id
			and A.plan_id = B.plan_id 
			inner join 
			WIP_ROUTE_GROUP_INOUT_V C with (NOLOCK) -- select * from WIP_ROUTE_GROUP_INOUT_V  with (nolock)
			on A.DMD_ATTB_6 = C.APS_WIP_ROUTE_GRP_ID
			and A.DMD_ATTB_13 = C.LAYER_INOUT
	where A.CORPORATION = 'STK' 
	and A.plant = 'STK' 
	and A.DMD_ATTB_1 = 'WIP'		
");

			if (terms["group_id"].Length > 0 )
			{
				sSQL.Append($@"
		AND DMD_ATTB_8 = {terms["group_id"].V} --##### UI 조회조건에서 입력
");
			}


			sSQL.Append($@"
),
result1 as (
	select  --A.*, B.*,C.*  
			distinct -- th_mst_item_route_site의 자사 외주 때문에 중복 나옴 
			A.master_id, A.plan_id, A.DIVISION_ID, A.JOB_NAME, A.ORG_ITEM_CODE, B.OPERATION_QTY, 
			A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTING_LAYER_INOUT,
			A.WRG_IO_ID, A.WRG_IO_NAME,
			A.PLAN_START_DTTM, A.TO_DTTM, A.FIRST_OPER_SEQ
	from    WIP_WRG_IO A
			inner join 
			th_out_order_tracking B with (NOLOCK)  -- select top 100 * from th_out_order_tracking where master_id = 'SIMMTECH' and plan_id = 'SIMM_20250917_P02' and Out_Version_id = 'SIMM_20250917_P02' and start_time between '2025-07-29 07:30:00' and '2025-07-30 07:30:00'
			on A.master_id = B.master_id
			and A.plan_id = B.plan_id 
			and A.plan_id = B.OUT_VERSION_ID
			and A.job_name = B.DEMAND_ID
			and B.ORDER_ID_TYPE = 'W' 
			and B.start_time between A.PLAN_START_DTTM and A.TO_DTTM
			inner join 
			th_mst_item_route_site C	with (NOLOCK) -- select * from th_mst_item_route_site  with (NOLOCK) where corporation ='STK' and plant = 'STK' and master_id = 'SIMMTECH' and plan_id = 'SIMM_20250917_P01' 
			on A.master_id = C.master_id 
			and A.plan_id = C.plan_id 
			and 'STK'  = C.corporation
			and 'STK'  = C.plant
			and A.job_name = C.IRS_ATTB_9
			and A.FIRST_OPER_SEQ = c.route_level -- TH_MST_DEMAND에서 현재 WIP OPER_SEQ, 단, COMPLETED 이면 다음 공정의 SEQ. Join 용. 추가함. 
			--and B.SITE_ID = C.site_id 
			--and B.OUT_ITEM_ID = C.ITEM_ID
	--where A.JOB_NAME = 'MH2570035-47G1'
	--and B.ORDER_ID_TYPE = 'W' 
	--and B.start_time between A.PLAN_START_DTTM and A.TO_DTTM
	where
		1=1
			and B.site_id not in ( 'V_I', 'V_O' )   
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
		AND A.DIVISION_ID = {terms["group_id"].V} --##### UI 조회조건에서 입력
");
            }

            if (terms["order_type"].Length > 0)
            {
                if (terms["order_type"].AsString() == "MASS") // 양산 M 
                {
                    sSQL.Append($@"
    AND LEFT(A.JOB_NAME, 1) = 'M'
");
                }
                else if (terms["order_type"].AsString() == "SAMPLE") // SAMPLE NOT T, NOT M 
                {
                    sSQL.Append($@"
    AND LEFT(A.JOB_NAME, 1) != 'T' 
    AND LEFT(A.JOB_NAME, 1) != 'M'
");
                }
                else if (terms["order_type"].AsString() == "TEST") // TEST T 
                {
                    sSQL.Append($@"
    AND LEFT(A.JOB_NAME, 1) = 'T'
");
                }
            }

            sSQL.Append($@"
) ,
--JOB NAME별로 동일 WRG_IO_ID에 대해 한 번만 수량 합산되게 하는 로직  
result2 as (
	select	A.DIVISION_ID, A.WRG_IO_ID, A.WRG_IO_NAME, A.JOB_NAME, A.ORG_ITEM_CODE, max(A.OPERATION_QTY) as LOT_QTY --   A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTING_LAYER_INOUT, -- , A.PLAN_START_DTTM, A.TO_DTTM, A.FIRST_OPER_SEQ 
	from	result1 A
	group by A.DIVISION_ID, A.WRG_IO_ID, A.WRG_IO_NAME, A.JOB_NAME, A.ORG_ITEM_CODE
), 
result3 as (
	select	A.DIVISION_ID, A.WRG_IO_ID, A.WRG_IO_NAME, A.JOB_NAME, A.ORG_ITEM_CODE, B.PATTERN_L, A.LOT_QTY 
	from	result2 A
			inner join
			(
				select ITEM_CODE, max(PATTERN_L) as PATTERN_L 
				from   (
					select ITEM_CODE, case when PATTERN = '' then 'Tenting' else 'MSAP' end as PATTERN_L 
					from   TH_GUI_ITEM_BY_PROCESS_GUBUN B with (nolock) 
					group by ITEM_CODE, case when PATTERN = '' then 'Tenting' else 'MSAP' end 
				) C
				group by ITEM_CODE
			) B
			on A.ORG_ITEM_CODE = B.ITEM_CODE
), 
result4 as (
	select A.DIVISION_ID, A.WRG_IO_ID, A.WRG_IO_NAME, A.PATTERN_L, sum(LOT_QTY) as WIP_STATUS_PLAN_QTY
	from result3 A
	group by A.DIVISION_ID, A.WRG_IO_ID, A.WRG_IO_NAME, A.PATTERN_L	
) 
");

            sSQL.Append($@"
,PIVOT_RESULT AS (
SELECT * 
FROM (
	SELECT DIVISION_ID, WRG_IO_NAME, PATTERN_L, CEILING(WIP_STATUS_PLAN_QTY) AS OPERATION_QTY
	FROM result4
) result
PIVOT (
	SUM(OPERATION_QTY) FOR WRG_IO_NAME IN (
		{result}
	)
) AS PIVOT_RESULT
),RST_PIVOT_TOTAL AS (
select 
	DIVISION_ID,
	'목표 예상' AS MAIN_CATEGORY,
	'생산계획 물량(APS)' AS CATEGORY_LEVEL1,
	PATTERN_L AS PATTERN,
	{totalColumn},
	{result3}
FROM
	PIVOT_RESULT
WHERE
	1=1
");
			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
	AND DIVISION_ID = {terms["group_id"].V}
");
			}

            sSQL.Append($@"
UNION ALL
	select 
		MAX(DIVISION_ID),
	    '목표 예상' AS MAIN_CATEGORY,
	    '생산계획 물량(APS)' AS CATEGORY_LEVEL1,
	    'TOTAL' AS PATTERN,
	    {sumTotalColumn},
		{result2}
	from
	    PIVOT_RESULT
	WHERE
		1=1
");

            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
	AND DIVISION_ID = {terms["group_id"].V}
");
            }

            sSQL.Append($@"
)
select * from RST_PIVOT_TOTAL;
");

            Console.WriteLine(sSQL.ToString());

			return Data.Get(sSQL.ToString()).Tables[0];
		}


		private DataTable SearchExpectationTarget(Params terms, ParamList headers)
		{
            DTClient.UserInfoMerge(terms);

            // pivot 값 넣기 위한 변수
			List<string> dataFieldList = new List<string>();
			// sum pivot 값 넣기 위한 변수
            List<string> dataFieldList2 = new List<string>();
            // select 조회를 위한 변수
            List<string> dataFieldList3 = new List<string>();
			// TOTAL 계산용
            List<string> dataFieldList4 = new List<string>();
            // SUM TOTAL 계산용
            List<string> dataFieldList5 = new List<string>();

            foreach (var item in headers)
            {
                dataFieldList.Add($"\"{item["label"].AsString()}\"");
                dataFieldList2.Add($"SUM(\"{item["label"].AsString()}\")");

				// "[IN] MLB" AS IN_MLB    이런형태가되어야함
				// "label" AS dataField
				dataFieldList3.Add($"\"{item["label"].AsString()}\" AS \"{item["dataField"].AsString()}\"");
                // TOTAL 계산용: ISNULL("label", 0)
                dataFieldList4.Add($"ISNULL(\"{item["label"].AsString()}\", 0)");
                // SUM TOTAL 계산용: ISNULL(SUM("label"), 0)
                dataFieldList5.Add($"ISNULL(SUM(\"{item["label"].AsString()}\"), 0)");

            }
			string result = string.Join(", ", dataFieldList);
            string result2 = string.Join(", ", dataFieldList2);
            string result3 = string.Join(", ", dataFieldList3);

            // TOTAL 열 계산식
            string totalExpression = string.Join(" + ", dataFieldList4);
            string totalColumn = $"{totalExpression} AS TOTAL";

            // SUM TOTAL 열 계산식
            string sumTotalExpression = string.Join(" + ", dataFieldList5);
            string sumTotalColumn = $"{sumTotalExpression} AS TOTAL";



            StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
with 
WRG_IO_WITH_PATTERN_TYPE_L1 as (
select A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.LAYER_INOUT, A.WRG_IO_ID, A.WRG_IO_NAME, A.SORT_ORDER, B.PATTERN_TYPE_L1
from WIP_ROUTE_GROUP_INOUT_V A WITH (NOLOCK)
 inner join 
 PATTER_TYPE_L1_LIST_V B
 on A.DIVISION_ID = B.DIVISION_ID
),
RESULT_TABLE AS (
	select 
		A.DIVISION_ID, 
		A.APS_WIP_ROUTE_GRP_ID, 
		A.WIP_ROUTE_GROUP_NAME, 
		A.LAYER_INOUT, --B.LAYER_INOUT as B_LAYER_INOUT, 
		A.WRG_IO_ID, 
		A.WRG_IO_NAME, 
		A.PATTERN_TYPE_L1, --B.PATTERN_TYPE_L1 as TT, 
		B.DAILY_PRODUCTION_TARGET_QTY, 
		A.SORT_ORDER 
	from 
		WRG_IO_WITH_PATTERN_TYPE_L1 A
		left outer join 
		TH_TAR_WRG_IO_PTN_DAILY_PROD_TARGET B WITH (NOLOCK)
		on 1=1
		and A.DIVISION_ID = B.DIVISION_ID
		and A.APS_WIP_ROUTE_GRP_ID = B.APS_WIP_ROUTE_GRP_ID
		and A.LAYER_INOUT = B.LAYER_INOUT
		and A.PATTERN_TYPE_L1 = B.PATTERN_TYPE_L1
		and B.use_yn = 'Y'
	where 
		1=1
)
,PIVOT_RESULT AS (
SELECT *
FROM (
        SELECT DIVISION_ID, WRG_IO_NAME, PATTERN_TYPE_L1, DAILY_PRODUCTION_TARGET_QTY
        FROM RESULT_TABLE 
) result
PIVOT (
        SUM(DAILY_PRODUCTION_TARGET_QTY) FOR WRG_IO_NAME IN (
			{result}
        )
) AS PIVOT_RESULT
),
rst_pivot_total as (
select 
	DIVISION_ID,
	'목표 예상' AS MAIN_CATEGORY,
    '일 생산목표' AS CATEGORY_LEVEL1,
    PATTERN_TYPE_L1 AS PATTERN,
	{totalColumn},
	{result3}
from 
	PIVOT_RESULT
WHERE 
	1=1
");
			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
	AND DIVISION_ID = {terms["group_id"].V}
");
			}

			sSQL.Append($@"
UNION ALL
	select 
		MAX(DIVISION_ID),
	    '목표 예상' AS MAIN_CATEGORY,
	    '일 생산목표' AS CATEGORY_LEVEL1,
	    'TOTAL' AS PATTERN,
	    {sumTotalColumn},
		{result2}
	from
	    PIVOT_RESULT
	WHERE
		1=1
");

            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
	AND DIVISION_ID = {terms["group_id"].V}
");
            }

			sSQL.Append($@"
)
select * from rst_pivot_total;
");

			//Console.WriteLine(sSQL.ToString());

			return Data.Get(sSQL.ToString()).Tables[0];
		}

		private DataTable SearchMonthAvailableQuantity(Params terms, ParamList headers)
		{
            DTClient.UserInfoMerge(terms);

            // pivot 값 넣기 위한 변수
            List<string> dataFieldList = new List<string>();
            // sum pivot 값 넣기 위한 변수
            List<string> dataFieldList2 = new List<string>();
            // select 조회를 위한 변수
            List<string> dataFieldList3 = new List<string>();
            // TOTAL 계산용
            List<string> dataFieldList4 = new List<string>();
            // SUM TOTAL 계산용
            List<string> dataFieldList5 = new List<string>();

            foreach (var item in headers)
            {
                dataFieldList.Add($"\"{item["label"].AsString()}\"");
                dataFieldList2.Add($"SUM(\"{item["label"].AsString()}\")");

                // "[IN] MLB" AS IN_MLB    이런형태가되어야함
                // "label" AS dataField
                dataFieldList3.Add($"\"{item["label"].AsString()}\" AS \"{item["dataField"].AsString()}\"");
                // TOTAL 계산용: ISNULL("label", 0)
                dataFieldList4.Add($"ISNULL(\"{item["label"].AsString()}\", 0)");
                // SUM TOTAL 계산용: ISNULL(SUM("label"), 0)
                dataFieldList5.Add($"ISNULL(SUM(\"{item["label"].AsString()}\"), 0)");

            }
            string result = string.Join(", ", dataFieldList);
            string result2 = string.Join(", ", dataFieldList2);
            string result3 = string.Join(", ", dataFieldList3);

            // TOTAL 열 계산식
            string totalExpression = string.Join(" + ", dataFieldList4);
            string totalColumn = $"{totalExpression} AS TOTAL";

            // SUM TOTAL 열 계산식
            string sumTotalExpression = string.Join(" + ", dataFieldList5);
            string sumTotalColumn = $"{sumTotalExpression} AS TOTAL";



            StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
with 
PST as (
	select master_id, plan_id, PLAN_START_DTTM, PLAN_START_DTTM+1 as TO_DTTM,
	       dateadd(month, 1, dateadd(day, 1-DAY(PLAN_START_DTTM), PLAN_START_DTTM)) as NEXT_MONTH_1ST_PST  --다음달 1일 시업시간 
	from th_eng_plan_info with (NOLOCK) 
	where master_id = 'SIMMTECH' 
	and plan_id = {terms["PLAN_ID"].V}  -- UI에서 생산계획 버전 선택 필요 ###########
	--order by plan_id desc
)
, 
cufoff_Date as (
select plan_id, PLAN_ATTB_1 as WIP_YYYYMMDD,  PLAN_ATTB_2 as WIP_SEQ
from  th_mst_plan with (nolock)
WHERE PLAN_ID = {terms["PLAN_ID"].V}
)
, 
-- 다음달 1일 시업시간 이전에 완료 공정(Virtual Out)이 시작(=종료)된 Lot 목록 --> item ID --> MST ITEM ROUTE SITE 와 엮어서 Job Name 추출할 것  
COMPLETE_IN_THIS_MONTH as (  
	select	A.MASTER_ID, A.PLAN_ID, A.OUT_VERSION_ID, A.WORK_ORDER_ID, A.RESOURCE_ID, A.ITEM_ID, A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, 
			A.START_TIME, A.END_TIME, A.OPERATION_TIME, A.TOTAL_WORKING_TIME, A.OPERATION_QTY, A.COMPLETE_QTY, A.BATCH_QTY, -- A.ORG_WORK_ORDER_ID, 
			--C.IRS_ATTB_11 as NEW_WIP_GBN, 
			B.PLAN_START_DTTM, B.NEXT_MONTH_1ST_PST
	from	th_out_work_order_SPL A with (NOLOCK)  -- SPLIT으로 하는게 맞을지? select top 10 * from  th_out_work_order where plan_id = 'SIMM_20250725_P01' 
			inner join 
			PST B 
			on A.master_id = B.master_id 
			and A.plan_id = B.plan_id 
			and A.OUT_VERSION_ID = B.PLAN_ID
			--## WIP만 대상으로 집계 2025.08.13. 수정 -->  아래에서 th_mst_item_route_site Join  
			--inner join -- left outer join select * from  [dbo].[TABLE_ATTRIBUTE_V]
			--th_mst_item_route_site C with (NOLOCK) 
			--on C.corporation = 'STK' 
			--and C.plant= 'STK' 
			--and A.master_id = C.master_id 
			--and A.plan_id = C.plan_id 
			--and A.ITEM_ID = C.ITEM_ID
			--and A.route_id = C.ROUTE_ID
			--and A.site_id = C.site_id 
	where 1=1 --A.master_id = 'SIMMTECH'
	and A.site_id = 'V_O' 
	and A.start_time <= B.NEXT_MONTH_1ST_PST
	--and C.IRS_ATTB_11 = 'WIP' --## WIP만 대상 조건 추가 
)
--select * from COMPLETE_IN_THIS_MONTH
, 
-- MST ITEM_ROUTE_SITE와 엮어서 JOB_NAME, ITEM_CODE, REVISION 가져옴
COMPLETE_IN_THIS_MONTH_ATTR as (
	select	A.MASTER_ID, A.PLAN_ID, A.OUT_VERSION_ID, A.WORK_ORDER_ID, A.RESOURCE_ID, A.ITEM_ID, A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, 
			A.START_TIME, A.END_TIME, A.OPERATION_TIME, A.TOTAL_WORKING_TIME, A.OPERATION_QTY, A.COMPLETE_QTY, A.BATCH_QTY, --A.ORG_WORK_ORDER_ID, 
			A.PLAN_START_DTTM, A.NEXT_MONTH_1ST_PST,
			B.IRS_ATTB_2 as ITEM_CODE, B.IRS_ATTB_3 as REVISION, 
			B.IRS_ATTB_9 as JOB_NAME  -- A.COMPLETE_QTY
	from	COMPLETE_IN_THIS_MONTH A
			inner join 
			TH_MST_ITEM_ROUTE_SITE B with (NOLOCK)   -- select * from TH_MST_ITEM_ROUTE_SITE where master_id = 'SIMMTECH' and plan_id = 'SIMM_20250725_P01' and item_id = 'HOT04570H00_HT2530023-01' and site_id = 'V_O' 
			on  B.corporation = 'STK' 
			and B.plant= 'STK' 
			and A.MASTER_ID = B.MASTER_ID
			and A.PLAN_ID = B.PLAN_ID	
			and A.ITEM_ID = B.ITEM_ID 
			and A.SITE_ID = B.SITE_ID 
			and A.ROUTE_ID = B.ROUTE_ID 
			--and A.OPERATION_ID = B.OPERATION_ID 
	where 1=1
	and B.IRS_ATTB_11 = 'WIP' --## 2025.08.13.  WIP만 대상 조건 추가 
)
--select * from COMPLETE_IN_THIS_MONTH_ATTR
, 
-- WRG 내외층 구분한 현재 LOT 위치 -------------------------------------------
WIP_LIST as (
        SELECT  -- A.YYYYMMDD,
                A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, -- 참고용
                A.WORKING_TYPE, -- HOLD 수량 별도로 집계. 값 목록 : HOLD / Waiting / Ongoing / Completed / Waiting to move / Moving   -- Move 관련 항목은 실제로는 없음.
                -- WORK_STATUS, ORGANIZATION_ID,
                A.DEPT_CODE, -- OPERATION_SEQ_NUM, SCH_DATE, FIRST_UNIT_START_DATE, COMP_DATE, COMP_DATE2, DELTA, WAIT_TIME,
                A.SQM, -- PRODUCT_WPNL, PRODUCT_PCS, PRODUCT_M2, SUBCONTRACTOR_FLAG, REPEAT,
                CASE WHEN A.INNER_OUTER = 'I' THEN 'INNER' ELSE 'OUTER' END AS INNER_OUTER,
                A.PATTERN, --INPUT_YIELD, INSERT_ID, INSERT_DTTM, UPDATE_ID, UPDATE_DTTM
                                B.PATTERN_TYPE_L1
        FROM    TH_TAR_WIP_HIS A  with (NOLOCK)  --> !!! 최신 WIP 대상으로만 조회됨 !!! --  select * from TH_TAR_WIP A where   A.yyyymmdd = '20250519' and A.seq = '002' and A.ORGANIZATION_ID = 101
                left outer join
                TH_TAR_DEPT_MASTER C with (NOLOCK)
                on A.dept_code = C.DEPARTMENT_CODE
                left outer join
                PATTER_TYPE_L2_LIST_V B with (NOLOCK)
                on c.DIVISION_ID = b.division_id
                and A.pattern = B.PATTERN_TYPE_L2
        where
            A.ORGANIZATION_ID = 101 -- 현재는 101 단일값.
	    	and A.yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
			and A.SEQ = (select WIP_SEQ from cufoff_Date)
			and A.USE_YN = 'Y'
");

            if (terms["order_type"].Length > 0)
            {
                if (terms["order_type"].AsString() == "MASS") // 양산 M 
                {
                    sSQL.Append($@"
    and LEFT(A.JOB_NAME, 1) = 'M' 
");
                }
                else if (terms["order_type"].AsString() == "SAMPLE") // SAMPLE NOT T, NOT M 
                {
                    sSQL.Append($@"
    AND LEFT(A.JOB_NAME, 1) != 'T' 
    AND LEFT(A.JOB_NAME, 1) != 'M'
");
                }
                else if (terms["order_type"].AsString() == "TEST") // TEST T 
                {
                    sSQL.Append($@"
    AND LEFT(A.JOB_NAME, 1) = 'T'
");
                }
            }

            sSQL.Append($@"
)
,
inout_group_mapping_raw as (
        select  B.DIVISION_ID, A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, A.WORKING_TYPE, A.DEPT_CODE, A.SQM, A.INNER_OUTER, A.PATTERN, A.PATTERN_TYPE_L1,
                B.APS_WIP_ROUTE_GRP_ID, B.WIP_ROUTE_GROUP_NAME
        from    WIP_LIST A
                inner join
                TH_TAR_DEPT_MASTER_WITH_NAME_V B with (NOLOCK) 
                on A.DEPT_CODE = B.dept_code
		WHERE 
			1=1
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
		AND B.DIVISION_ID = {terms["group_id"].V}
");
            }

            sSQL.Append($@"
)
, 
item_yield as (
	select ITEM_CODE, (1.0-isnull(convert(float, IPG.SHRINKAGE_RATE), 0.0)) AS YIELD  -- 수율 
	from   TH_GUI_ITEM_BY_PROCESS_GUBUN IPG with (nolock)
)
,
WIP_COMPLETE_IN_THIS_MONTH_ATTR as (
	select	A.DIVISION_ID, A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, A.WORKING_TYPE, A.DEPT_CODE, 
			A.SQM, -- 현재 WIP 공정 위치 수량 
			A.INNER_OUTER, A.PATTERN, 
			A.PATTERN_TYPE_L1,
			A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, 
			--B.* --B.COMPLETE_QTY 완성 수량 
			--B.MASTER_ID, B.PLAN_ID, B.OUT_VERSION_ID, B.WORK_ORDER_ID, B.RESOURCE_ID, B.ITEM_ID, B.SITE_ID, B.ROUTE_ID, B.OPERATION_ID, 
			--B.START_TIME, B.END_TIME, B.OPERATION_TIME, B.TOTAL_WORKING_TIME, B.OPERATION_QTY, B.COMPLETE_QTY, B.BATCH_QTY, A.ORG_WORK_ORDER_ID, A.PLAN_START_DTTM, A.NEXT_MONTH_1ST_PST,
			--B.ITEM_CODE as ITEM_CODE_B, -- B.IRS_ATTB_3 as REVISION, 
			--B.JOB_NAME  
			--B.COMPLETE_QTY,	-- 완성 공정 수량 
			B.COMPLETE_QTY * D.YIELD AS COMPLETE_QTY,
			C.WRG_IO_ID, C.WRG_IO_NAME, C.SORT_ORDER
	from	inout_group_mapping_raw A  -- WRG 내외층 구분한 현재 LOT 위치 --> 금월 내 완료 가능한 JOB NAME들만 수량 집계할 것   
			inner join 
			COMPLETE_IN_THIS_MONTH_ATTR B
			on A.JOB_NAME = B.JOB_NAME
			inner join  
			WIP_ROUTE_GROUP_INOUT_V C with (NOLOCK)  -- select * from WIP_ROUTE_GROUP_INOUT_V
			on A.DIVISION_ID = C.DIVISION_ID
			and A.APS_WIP_ROUTE_GRP_ID = C.APS_WIP_ROUTE_GRP_ID
			and A.INNER_OUTER = C.LAYER_INOUT
			left join
            item_yield D
            on A.ITEM_CODE = D.ITEM_CODE 
	where 
		1=1
");
			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
		AND A.DIVISION_ID = {terms["group_id"].V}
");
			}

			sSQL.Append($@"
) 
, 
-- division, WIP_ROUTE_GROUP_INOUT_V, Pattern L1 기준으로 집계 
WIP_IO_COMPLETE_IN_THIS_MONTH_ATTR as (  -- 있는 것만 나오는 것
	select	A.DIVISION_ID, --A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, A.WORKING_TYPE, A.DEPT_CODE, 
			A.WRG_IO_ID, A.WRG_IO_NAME, A.SORT_ORDER,		
			--A.SQM, -- 현재 WIP 공정 위치 수량 
			--A.INNER_OUTER, 
			A.PATTERN, 
			A.PATTERN_TYPE_L1,
			--A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME,  
			sum(A.COMPLETE_QTY) as complete_qty_in_this_month  --금월 입고 예정 완성 수량 
	from WIP_COMPLETE_IN_THIS_MONTH_ATTR A
	group by A.DIVISION_ID, A.WRG_IO_ID, A.WRG_IO_NAME, A.INNER_OUTER, A.SORT_ORDER, A.PATTERN_TYPE_L1, A.PATTERN
) 
, rst as (
SELECT  A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.LAYER_INOUT,
                A.WRG_IO_ID, A.WRG_IO_NAME, A.SORT_ORDER, C.PATTERN_TYPE_L1
--                , C.PATTERN_GUBUN
                , SUM(B.COMPLETE_QTY_IN_THIS_MONTH) AS COMPLETE_QTY_IN_THIS_MONTH
FROM    WIP_ROUTE_GROUP_INOUT_V A -- SELECT * FROM WIP_ROUTE_GROUP_INOUT_V
        INNER JOIN -- select * from PATTER_TYPE_L2_LIST_V
        PATTER_TYPE_L2_LIST_V C
        ON A.DIVISION_ID = C.DIVISION_ID
        left outer JOIN
        WIP_IO_COMPLETE_IN_THIS_MONTH_ATTR B
        ON A.DIVISION_ID = B.DIVISION_ID
        AND A.WRG_IO_ID = B.WRG_IO_ID
        AND C.PATTERN_TYPE_L2 = B.PATTERN
WHERE 1=1
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
		AND A.DIVISION_ID = {terms["group_id"].V}
");
            }


            sSQL.Append($@"
group by A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.LAYER_INOUT,
                A.WRG_IO_ID, A.WRG_IO_NAME, A.SORT_ORDER, C.PATTERN_TYPE_L1
) 
,
rst_pivot_base AS (
        SELECT *
        FROM (
                SELECT '목표 예상' AS MAIN_CATEGORY, PATTERN_TYPE_L1, WRG_IO_NAME, CEILING(COMPLETE_QTY_IN_THIS_MONTH) AS COMPLETE_QTY_IN_THIS_MONTH
                FROM rst
        ) result
        PIVOT (
                SUM(COMPLETE_QTY_IN_THIS_MONTH) FOR WRG_IO_NAME IN (
                        {result}
                )
        ) AS PIVOT_RESULT
) 
,RST_PIVOT_TOTAL AS (
select 
	MAIN_CATEGORY,
	'금월 생산 입고 가능물량' AS CATEGORY_LEVEL1,
	PATTERN_TYPE_L1 AS PATTERN,
	{totalColumn},
	{result3}
from 
	rst_pivot_base
");

			sSQL.Append($@"
UNION ALL
	select 
	    '목표 예상' AS MAIN_CATEGORY,
	    '금월 생산 입고 가능물량' AS CATEGORY_LEVEL1,
	    'TOTAL' AS PATTERN,
	    {sumTotalColumn},
		{result2}
	from
	    rst_pivot_base
	WHERE
		1=1
");
			sSQL.Append($@"
)
select * from rst_pivot_total;
");
			Console.WriteLine(sSQL.ToString());

			return Data.Get(sSQL.ToString()).Tables[0];
		}


		private DataTable search_summary_actual(Params terms)
		{
			StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
SELECT 
	DIVISION_ID,
    PATTERN AS TYPE, 
    TARGET
FROM (
    SELECT 
		DIVISION_ID,
        PATTERN, 
        TARGET,
        ROW_NUMBER() OVER (PARTITION BY PATTERN ORDER BY INSERT_DTTM DESC) AS rn
    FROM TH_GUI_WIP_STATUS_TARGET
    WHERE
    	1=1
    	AND DIVISION_ID = {terms["group_id"].V}
) AS sub
WHERE rn = 1
UNION ALL
SELECT 
	MAX(DIVISION_ID) AS DIVISION_ID,
    'TOTAL' AS TYPE,
    SUM(TRY_CAST(TARGET AS FLOAT)) AS TARGET
FROM (
    SELECT 
		DIVISION_ID,
        PATTERN, 
        TARGET,
        ROW_NUMBER() OVER (PARTITION BY PATTERN ORDER BY INSERT_DTTM DESC) AS rn
    FROM TH_GUI_WIP_STATUS_TARGET
    WHERE
    	1=1
    	AND DIVISION_ID = {terms["group_id"].V}
) AS sub
WHERE rn = 1
");

			return Data.Get(sSQL.ToString()).Tables[0];
		}

        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
			data.ForEach(item =>
			{
                StringBuilder sSQL = new StringBuilder();
				if(item["TYPE"].AsString() != "TOTAL")
				{
                    sSQL.Append($@"
INSERT INTO TH_GUI_WIP_STATUS_TARGET(PATTERN, TARGET, INSERT_ID, INSERT_DTTM, DIVISION_ID)
VALUES ({item["TYPE"].V}, {item["TARGET"].V}, '{Cookie<User>.Store.USER_ID}', '{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}', {item["DIVISION_ID"].V})
");
                }

                if (sSQL.Length > 0)
                {
                    HS.Web.Common.Data.Execute(sSQL.ToString());
                }
			});
			
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
        /// 그리드 헤더컬럼 옵션 저장
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveGrid(ParamList dataList)
        {
            HS.Web.Proc.SI_GRID.Save(dataList);
        }


    }
}
