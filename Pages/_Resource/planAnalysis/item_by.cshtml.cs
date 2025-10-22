//using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class item_by : BasePageModel
    {

        public item_by()
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

                toClient["data"] = this.SearchBasic(terms);
                toClient["dataCompleteable"] = this.SearchCompleteable(terms);
                toClient["dataRemaingStock"] = this.SearchRemaingStock(terms);
                toClient["dataTotalWip"] = this.SearchTotalWip(terms);
                toClient["dataHoldCount"] = this.SearchHoldCount(terms);
                toClient["dataCurrentWip"] = this.SearchCurrentWip(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

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
        private DataTable SearchBasic(Params terms)
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

            var sSQL = new StringBuilder();

            sSQL.Append($@"
with rst AS (
select	A.DMD_ATTB_8 as DIVISION_ID, 
		A.DMD_ATTB_2 as ITEM_CODE, 
		B.MODEL_NAME,
        C.CUSTOMER,
		CONVERT(DATE, A.DUE_DATE) AS BASE_DATE,  -- MST_DEMAND 납기는 시업시간 미반영되어 있음. 반영되면 CONVERT(DATE, DATEADD(MINUTE, -450, A.DUE_DATE)) AS BASE_DATE, 형태로 변경할 것. 
		sum(A.QTY) as SHIPPING_QTY
from th_dpp_net_demand A  -- select distinct substring(DEMAND_ID, 1,1) from th_mst_demand where CORPORATION = 'STK'  and PLANT = 'STK'  and master_id = 'SIMMTECH'  and PLAN_ID = 'SIMM_20250730_P06' 
	 left join CBST_SPEC_BASIC B
	 ON 1=1
     AND B.ORGANIZATION_ID = '101'
	 AND A.DMD_ATTB_2 = B.ITEM_CODE
	 AND B.REVISION = (SELECT MAX(REVISION) FROM CBST_SPEC_BASIC WHERE ITEM_CODE = A.DMD_ATTB_2 )
	 left join TH_GUI_ITEM_MODEL_SEARCH C
	 ON 1=1
	 ANd A.DMD_ATTB_2 = C.ITEM_CODE
where CORPORATION = 'STK' 
and PLANT = 'STK' 
and master_id = 'SIMMTECH' 
and PLAN_ID = {terms["PLAN_ID"].V}
--and DUE_DATE between {terms["start_date"].V} and  {terms["end_date"].V}  --## UI 필터 설정 기간
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
and A.DMD_ATTB_8 = {terms["group_id"].V}
");
            }

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
and A.DMD_ATTB_2 LIKE '%{terms["item_code"].AsString()}%'
");
            }

            if (terms["customer"].Length > 0)
            {
                sSQL.Append($@"
and C.CUSTOMER LIKE '%{terms["customer"].AsString()}%'
");
            }

            sSQL.Append($@"
group by A.DMD_ATTB_8, A.DMD_ATTB_2, B.MODEL_NAME, A.DUE_DATE, C.CUSTOMER
) 
select *
from ( 
		select DIVISION_ID, ITEM_CODE, '납기' AS GUBUN, MODEL_NAME, CUSTOMER, BASE_DATE, SHIPPING_QTY
		from rst
	 ) as result
PIVOT (
        sum(SHIPPING_QTY) for BASE_DATE
		in (
				{result}
			)
		) as PIVOT_RESULT 
");

            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchCompleteable(Params terms) {

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


            var sSQL = new StringBuilder();

            sSQL.Append($@"
-- [2] 완성 가능 항목 
--     : th_out_work_order 완성공정 start date 날짜로 (WIP + 신규)
--            Inv 없음. WIP, 신규 완성 예상만 구분하여 산출 

-- 완성 가능 WIP, 신규투입 구분, WIP 현재 공정 위치 (LOT별 현재 WIP Route Group 내외층 구분 기준 가장 느린 것 위치 (내층>외층, sort order 빠른 것)
-- WIP_NEW_GBN,--### 테스트용 임시 수정. 신규 샘플데이터가 없어서 테스트용으로 넣은 부분. .

with cufoff_Date as (
select plan_id, PLAN_ATTB_1 as WIP_YYYYMMDD,  PLAN_ATTB_2 as WIP_SEQ
from  th_mst_plan with (nolock)
WHERE PLAN_ID = {terms["PLAN_ID"].V}
)
-- LOT별 현재 WRG_IO 위치 (WRG 내외층 구분) -------------------------------------------
,WIP_STAGE as (
        SELECT  -- A.YYYYMMDD,
                A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, -- 참고용
                A.WORKING_TYPE, -- HOLD 수량 별도로 집계. 값 목록 : HOLD / Waiting / Ongoing / Completed / Waiting to move / Moving   -- Move 관련 항목은 실제로는 없음.
                -- WORK_STATUS, ORGANIZATION_ID,
                A.DEPT_CODE, -- OPERATION_SEQ_NUM, SCH_DATE, FIRST_UNIT_START_DATE, COMP_DATE, COMP_DATE2, DELTA, WAIT_TIME,
                A.SQM, -- PRODUCT_WPNL, PRODUCT_PCS, PRODUCT_M2, SUBCONTRACTOR_FLAG, REPEAT,
                CASE WHEN A.INNER_OUTER = 'I' THEN 'INNER' ELSE 'OUTER' END AS INNER_OUTER								
				, C.division_id, C.APS_WIP_ROUTE_GRP_ID, D.WRG_IO_ID, D.WRG_IO_NAME, D.SORT_ORDER				
        FROM    TH_TAR_WIP_HIS A
				left outer join 
				TH_TAR_DEPT_MASTER C 
				on A.dept_code = C.DEPARTMENT_CODE
				left outer join
				WIP_ROUTE_GROUP_INOUT_V D -- select * from WIP_ROUTE_GROUP_INOUT_V
				on C.division_id = D.DIVISION_ID
				and C.APS_WIP_ROUTE_GRP_ID = D.APS_WIP_ROUTE_GRP_ID
				and CASE WHEN A.INNER_OUTER = 'I' THEN 'INNER' ELSE 'OUTER' END = D.LAYER_INOUT	
		where 1=1
			and A.yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
			and A.SEQ = (select WIP_SEQ from cufoff_Date)
			and A.USE_YN = 'Y'   --  임시  -- select * from TH_TAR_WIP -- 하나의 Set만 들어있음 
) 
--select * from WIP_STAGE order by  JOB_NAME;
,
COMPLETE_ORG as (
   -- DEMAND TYPE 구분 필요? --> Demand Type 구분 안함
	select  C.ITEM_ID, C.START_TIME, -- C.END_TIME, C.OPERATION_TIME, C.TOTAL_WORKING_TIME, C.OPERATION_QTY, 
			--C.START_TIME, 
			CONVERT(DATE, DATEADD(MINUTE, -450, C.START_TIME)) AS BASE_DATE, -- 시업시간 기준 출고가능일  --> 이렇게 하는게 맞는지 확인할 것 
			C.COMPLETE_QTY, 
--			D.DMD_ATTB_1 as WIP_NEW_GBN,
			case when C.item_id like 'S%' or C.item_id like 'Z%' then 'NEW' else 'WIP' end as WIP_NEW_GBN,--### 테스트용 임시 수정
			D.DMD_ATTB_2 as ITEM_ID_ORG, 
			D.DEMAND_ID
	from	th_out_work_order C with (NOLOCK) 
			left outer join 
			th_dpp_net_demand D  with (NOLOCK) -- select * from  th_mst_demand  where master_id = 'SIMMTECH' and PLAN_ID = 'SIMM_20250730_P05'
			on  D.CORPORATION = 'STK' 
			and D.PLANT = 'STK' 
			and C.master_id = D.master_id 
			and C.plan_id = D.plan_id 
			and C.item_id = D.item_id 
	where 1=1
	and C.master_id = 'SIMMTECH' 
	and C.PLAN_ID = {terms["PLAN_ID"].V}
	and C.OUT_VERSION_ID = {terms["PLAN_ID"].V}
	and C.SITE_ID = 'V_O'
	and C.RESOURCE_ID = 'V_O'
	and C.START_TIME between {terms["start_date"].V} and  {terms["end_date"].V}  --## UI 필터 설정 기간 
) --select * from COMPLETE_ORG;
, 
COMPLETE_ORG_SUM as (
	select ITEM_ID_ORG, BASE_DATE, WIP_NEW_GBN, sum(COMPLETE_QTY) as COMPLETE_QTY_SUM
	from COMPLETE_ORG A 
	group by ITEM_ID_ORG, BASE_DATE, WIP_NEW_GBN
	--order by ITEM_ID_ORG, BASE_DATE, WIP_NEW_GBN
)
, 
-----------------------------------------
-- 출고가능에 WIP 위치 추가, 위치 랭킹  
COMPLETE_ORG_WIP_STAGE as (
	select	--A.*, 
			A.ITEM_ID_ORG,DEMAND_ID, A.BASE_DATE, A.WIP_NEW_GBN, A.COMPLETE_QTY, 
			B.APS_WIP_ROUTE_GRP_ID, B.INNER_OUTER, B.WRG_IO_ID, B.WRG_IO_NAME, B.SORT_ORDER			
	from	COMPLETE_ORG A 
			left outer join 
			WIP_STAGE B
			on A.DEMAND_ID = B.JOB_NAME   -- A.DEMAND_ID  --> Job_name
) 
--select * from COMPLETE_ORG_WIP_STAGE;
,
-- WIP에 대해서만 위치 랭킹  
wip_stage_rank as (
	select	A.*, 
			rank() over (partition by A.ITEM_ID_ORG, A.BASE_DATE order by INNER_OUTER, SORT_ORDER, demand_id) as wip_stage_rnk
	from COMPLETE_ORG_WIP_STAGE A
	where A.wip_new_gbn = 'WIP' 
	--and item_id_org = 'BOC05371C00'
)
, 
latest_wip_stage as (
	select * from wip_stage_rank 
	where wip_stage_rnk = 1 
) 
-----------------------------------------
,
rst as (
select  --A.*, B.*
                A.ITEM_ID_ORG AS ITEM_CODE, C.CUSTOMER, A.BASE_DATE, A.WIP_NEW_GBN, A.COMPLETE_QTY_SUM, B.WRG_IO_ID, B.WRG_IO_NAME
from    COMPLETE_ORG_SUM A
                left outer join
                latest_wip_stage B
                on A.ITEM_ID_ORG = B.ITEM_ID_ORG
                and A.BASE_DATE = B.BASE_DATE
                and A.WIP_NEW_GBN = B.WIP_NEW_GBN
                left outer join TH_GUI_ITEM_MODEL_SEARCH C
                on 1=1
                and A.ITEM_ID_ORG = C.ITEM_CODE
)
select *
from (
        select
                ITEM_CODE,
                --CUSTOMER,
                CASE WHEN WIP_NEW_GBN = 'NEW' THEN '└ 신규'
                        ELSE '┣ WIP' END
                AS GUBUN,
                BASE_DATE, COMPLETE_QTY_SUM, WIP_NEW_GBN
        from rst
        where 1=1
");
            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
    and ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
            }
            if (terms["customer"].Length > 0)
            {
                sSQL.Append($@"
    and CUSTOMER LIKE '%{terms["customer"].AsString()}%'
");
            }

            sSQL.Append(@$"
	 ) as result
PIVOT (
        sum(COMPLETE_QTY_SUM) for BASE_DATE
		in (
				{result}
			)
		) as PIVOT_RESULT 
");

            //Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchRemaingStock(Params terms) {

            var sSQL = new StringBuilder();

            sSQL.Append($@"
-- [4] 입고잔량 = 전체 기간 납기 합계
select	A.DMD_ATTB_8 as DIVISION_ID, 
	A.DMD_ATTB_2 as ITEM_CODE, 
	--A.DMD_ATTB_1 as WIP_INPUT_GBN, 
	sum(A.QTY) as TOTAL_DEMAND_QTY_SQM
	--A.DMD_ATTB_4 as WORK_TYPE, -- HOLD, Waiting, Completed, Ongoing 
from th_dpp_net_demand A  -- select distinct substring(DEMAND_ID, 1,1) from th_mst_demand where CORPORATION = 'STK'  and PLANT = 'STK'  and master_id = 'SIMMTECH'  and PLAN_ID = 'SIMM_20250730_P06' 
where CORPORATION = 'STK' 
and PLANT = 'STK' 
and master_id = 'SIMMTECH' 
and PLAN_ID = {terms["PLAN_ID"].V}
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
and A.DMD_ATTB_8 = {terms["group_id"].V}
");
            }

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
and A.DMD_ATTB_2 LIKE '%{terms["item_code"].AsString()}%'
");
            }


            sSQL.Append($@"
group by A.DMD_ATTB_8, A.DMD_ATTB_2
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchTotalWip(Params terms)
        {
            var sSQL = new StringBuilder();

            sSQL.Append($@"
with 
COMPLETE_ORG as (
   -- DEMAND TYPE 구분 필요? --> Demand Type 구분 안함
	select  D.DMD_ATTB_8 AS DIVISION_ID,
            C.out_ITEM_ID as item_id, 
			C.COMPLETE_QTY, 
			D.DMD_ATTB_1 as WIP_NEW_GBN,
			--## 테스트용 임시 수정
			--case when C.item_id like 'S%' or C.item_id like 'Z%' then 'NEW' else 'WIP' end as WIP_NEW_GBN,--### 테스트용 임시 수정
			D.DMD_ATTB_2 as ITEM_ID_ORG, 
			D.DEMAND_ID
	from	th_out_order_tracking C with (NOLOCK) 
			inner join -- left outer join 
			th_dpp_net_demand D  with (NOLOCK) -- select * from  th_dpp_net_demand  where master_id = 'SIMMTECH' and PLAN_ID = 'SIMM_20251015_M01' and ITEM_ID LIKE '%BOC04640A00%'
			on D.CORPORATION = 'STK' 
			and D.PLANT = 'STK' 
			and C.master_id = D.master_id 
			and C.plan_id = D.plan_id 
			and C.out_ITEM_ID = D.item_id 
			and c.demand_id = d.demand_id 
	where 1=1
        and C.master_id = 'SIMMTECH' 
        and C.PLAN_ID = {terms["PLAN_ID"].V}
        and C.OUT_VERSION_ID = {terms["PLAN_ID"].V}
        and C.SITE_ID = 'V_O'
        and C.RESOURCE_ID = 'V_O'
), 
EXPECTED_COMPLETE_WIP_NEW_SUM as (
select DIVISION_ID, ITEM_ID_ORG AS ITEM_CODE, WIP_NEW_GBN, sum(COMPLETE_QTY) as COMPLETE_QTY_SUM
from COMPLETE_ORG A 
group by DIVISION_ID, ITEM_ID_ORG, WIP_NEW_GBN
--order by ITEM_ID_ORG, BASE_DATE, WIP_NEW_GBN
)
select	A.*, 
	case when WIP_NEW_GBN = 'WIP' then '전체WIP'
			when WIP_NEW_GBN = 'NEW' then '투입잔량'
	end as column_name
from EXPECTED_COMPLETE_WIP_NEW_SUM A
WHERE 1=1
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
AND DIVISION_ID = {terms["group_id"].V}
");
            }

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
AND ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
            }

            sSQL.Append($@"
order by ITEM_CODE, WIP_NEW_GBN
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchHoldCount(Params terms)
        {
            var sSQL = new StringBuilder();

            sSQL.Append($@"
-- [7] HOLD WIP = WIP 중에서 WIP Status가 Hold인 수량 
select	A.DMD_ATTB_8 as DIVISION_ID, 
	A.DMD_ATTB_2 as ITEM_CODE, 
	--A.DMD_ATTB_1 as WIP_INPUT_GBN, 
	sum(A.QTY) as HOLD_WIP_QTY_SQM
	--A.DMD_ATTB_4 as WORK_TYPE, -- HOLD, Waiting, Completed, Ongoing 
from th_dpp_net_demand A  -- select distinct substring(DEMAND_ID, 1,1) from th_mst_demand where CORPORATION = 'STK'  and PLANT = 'STK'  and master_id = 'SIMMTECH'  and PLAN_ID = 'SIMM_20250730_P06' 
where CORPORATION = 'STK' 
and PLANT = 'STK' 
and master_id = 'SIMMTECH' 
and PLAN_ID = {terms["PLAN_ID"].V}
and A.DMD_ATTB_1 = 'WIP' 
and A.DMD_ATTB_4 = 'HOLD' 
");
            if (terms["groupo_id"].Length > 0)
            {
                sSQL.Append($@"
AND A.DMD_ATTB_8 = {terms["group_id"].V}
");
            }

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
AND A.DMD_ATTB_2 LIKE '%{terms["item_code"].AsString()}%'
");
            }

            sSQL.Append($@"
group by A.DMD_ATTB_8, A.DMD_ATTB_2 
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchCurrentWip(Params terms)
        {
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


            var sSQL = new StringBuilder();

            sSQL.Append($@"
-- [2] 완성 가능 항목 
--     : th_out_work_order 완성공정 start date 날짜로 (WIP + 신규)
--            Inv 없음. WIP, 신규 완성 예상만 구분하여 산출 

-- 완성 가능 WIP, 신규투입 구분, WIP 현재 공정 위치 (LOT별 현재 WIP Route Group 내외층 구분 기준 가장 느린 것 위치 (내층>외층, sort order 빠른 것)
-- WIP_NEW_GBN,--### 테스트용 임시 수정. 신규 샘플데이터가 없어서 테스트용으로 넣은 부분. .

with cufoff_Date as (
select plan_id, PLAN_ATTB_1 as WIP_YYYYMMDD,  PLAN_ATTB_2 as WIP_SEQ
from  th_mst_plan with (nolock)
WHERE PLAN_ID = {terms["PLAN_ID"].V}
)
-- LOT별 현재 WRG_IO 위치 (WRG 내외층 구분) -------------------------------------------
,WIP_STAGE as (
        SELECT  -- A.YYYYMMDD,
                A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, -- 참고용
                A.WORKING_TYPE, -- HOLD 수량 별도로 집계. 값 목록 : HOLD / Waiting / Ongoing / Completed / Waiting to move / Moving   -- Move 관련 항목은 실제로는 없음.
                -- WORK_STATUS, ORGANIZATION_ID,
                A.DEPT_CODE, -- OPERATION_SEQ_NUM, SCH_DATE, FIRST_UNIT_START_DATE, COMP_DATE, COMP_DATE2, DELTA, WAIT_TIME,
                A.SQM, -- PRODUCT_WPNL, PRODUCT_PCS, PRODUCT_M2, SUBCONTRACTOR_FLAG, REPEAT,
                CASE WHEN A.INNER_OUTER = 'I' THEN 'INNER' ELSE 'OUTER' END AS INNER_OUTER								
				, C.division_id, C.APS_WIP_ROUTE_GRP_ID, D.WRG_IO_ID, D.WRG_IO_NAME, D.SORT_ORDER				
        FROM    TH_TAR_WIP_HIS A
				left outer join 
				TH_TAR_DEPT_MASTER C 
				on A.dept_code = C.DEPARTMENT_CODE
				left outer join
				WIP_ROUTE_GROUP_INOUT_V D -- select * from WIP_ROUTE_GROUP_INOUT_V
				on C.division_id = D.DIVISION_ID
				and C.APS_WIP_ROUTE_GRP_ID = D.APS_WIP_ROUTE_GRP_ID
				and CASE WHEN A.INNER_OUTER = 'I' THEN 'INNER' ELSE 'OUTER' END = D.LAYER_INOUT	
		where 1=1
			and A.yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
			and A.SEQ = (select WIP_SEQ from cufoff_Date)
			and A.USE_YN = 'Y'   --  임시  -- select * from TH_TAR_WIP -- 하나의 Set만 들어있음 
) 
--select * from WIP_STAGE order by  JOB_NAME;
,
COMPLETE_ORG as (
   -- DEMAND TYPE 구분 필요? --> Demand Type 구분 안함
	select  C.ITEM_ID, C.START_TIME, -- C.END_TIME, C.OPERATION_TIME, C.TOTAL_WORKING_TIME, C.OPERATION_QTY, 
			--C.START_TIME, 
			CONVERT(DATE, DATEADD(MINUTE, -450, C.START_TIME)) AS BASE_DATE, -- 시업시간 기준 출고가능일  --> 이렇게 하는게 맞는지 확인할 것 
			C.COMPLETE_QTY, 
--			D.DMD_ATTB_1 as WIP_NEW_GBN,
			case when C.item_id like 'S%' or C.item_id like 'Z%' then 'NEW' else 'WIP' end as WIP_NEW_GBN,--### 테스트용 임시 수정
			D.DMD_ATTB_2 as ITEM_ID_ORG, 
			D.DEMAND_ID
	from	th_out_work_order C with (NOLOCK) 
			left outer join 
			th_dpp_net_demand D  with (NOLOCK) -- select * from  th_mst_demand  where master_id = 'SIMMTECH' and PLAN_ID = 'SIMM_20250730_P05'
			on  D.CORPORATION = 'STK' 
			and D.PLANT = 'STK' 
			and C.master_id = D.master_id 
			and C.plan_id = D.plan_id 
			and C.item_id = D.item_id 
	where 1=1
	and C.master_id = 'SIMMTECH' 
	and C.PLAN_ID = {terms["PLAN_ID"].V}
	and C.OUT_VERSION_ID = {terms["PLAN_ID"].V}
	and C.SITE_ID = 'V_O'
	and C.RESOURCE_ID = 'V_O'
	and C.START_TIME between {terms["start_date"].V} and  {terms["end_date"].V}  --## UI 필터 설정 기간 
) --select * from COMPLETE_ORG;
, 
COMPLETE_ORG_SUM as (
	select ITEM_ID_ORG, BASE_DATE, WIP_NEW_GBN, sum(COMPLETE_QTY) as COMPLETE_QTY_SUM
	from COMPLETE_ORG A 
	group by ITEM_ID_ORG, BASE_DATE, WIP_NEW_GBN
	--order by ITEM_ID_ORG, BASE_DATE, WIP_NEW_GBN
)
, 
-----------------------------------------
-- 출고가능에 WIP 위치 추가, 위치 랭킹  
COMPLETE_ORG_WIP_STAGE as (
	select	--A.*, 
			A.ITEM_ID_ORG,DEMAND_ID, A.BASE_DATE, A.WIP_NEW_GBN, A.COMPLETE_QTY, 
			B.APS_WIP_ROUTE_GRP_ID, B.INNER_OUTER, B.WRG_IO_ID, B.WRG_IO_NAME, B.SORT_ORDER			
	from	COMPLETE_ORG A 
			left outer join 
			WIP_STAGE B
			on A.DEMAND_ID = B.JOB_NAME   -- A.DEMAND_ID  --> Job_name
) 
--select * from COMPLETE_ORG_WIP_STAGE;
,
-- WIP에 대해서만 위치 랭킹  
wip_stage_rank as (
	select	A.*, 
			rank() over (partition by A.ITEM_ID_ORG, A.BASE_DATE order by INNER_OUTER, SORT_ORDER, demand_id) as wip_stage_rnk
	from COMPLETE_ORG_WIP_STAGE A
	where A.wip_new_gbn = 'WIP' 
	--and item_id_org = 'BOC05371C00'
)
, 
latest_wip_stage as (
	select * from wip_stage_rank 
	where wip_stage_rnk = 1 
) 
-----------------------------------------
,
rst as (
select	--A.*, B.* 
		A.ITEM_ID_ORG AS ITEM_CODE, C.CUSTOMER, A.BASE_DATE, A.WIP_NEW_GBN, A.COMPLETE_QTY_SUM, B.WRG_IO_ID, B.WRG_IO_NAME
from	COMPLETE_ORG_SUM A
		left outer join 
		latest_wip_stage B
		on A.ITEM_ID_ORG = B.ITEM_ID_ORG
		and A.BASE_DATE = B.BASE_DATE
		and A.WIP_NEW_GBN = B.WIP_NEW_GBN
        left outer join TH_GUI_ITEM_MODEL_SEARCH C
        on 1=1
        and A.ITEM_ID_ORG = C.ITEM_CODE
) --select * from rst;
select *
from ( 
		select 
			ITEM_CODE, 
            --CUSTOMER,
			'WIP 현재 공정 위치' AS GUBUN, 		
			BASE_DATE, WIP_NEW_GBN, WRG_IO_NAME
		from rst
		where 1=1
");

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
    and ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
            }
            if (terms["customer"].Length > 0)
            {
                sSQL.Append($@"
    and CUSTOMER LIKE '%{terms["customer"].AsString()}%'
");
            }

            sSQL.Append(@$"
	 ) as result
PIVOT (
        MAX(WRG_IO_NAME) for BASE_DATE
		in (
				{result}
			)
		) as PIVOT_RESULT 
");

            //Console.WriteLine(sSQL.ToString());


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
    }
}
