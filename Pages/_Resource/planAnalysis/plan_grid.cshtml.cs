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
    public class plan_grid : BasePageModel
    {

        public plan_grid()
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

                toClient["data"] = this.Search(terms);
                toClient["dataProdTarget"] = this.searchProdTarget(terms);
                toClient["searchLotTAT"] = this.searchLotTAT(terms);
                toClient["searchLotTATDetail"] = this.searchLotTATDetail(terms);
                toClient["searchProdTargetDetail"] = this.searchProdTargetDetail(terms);
            }


			else if (e.Command == "view")
			{
				Params terms = e.Params["terms"];

				//toClient["data"] = this.Search(terms);
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
with 
OTD_LIST as (
	select	B.DMD_ATTB_8 AS DIVISION_ID, A.demand_id, B.DUE_DATE, A.END_TIME, 
			case when A.END_TIME <= B.DUE_DATE then 'ON_TIME' 
				 when A.END_TIME > B.DUE_DATE then 'LATE'
				 else 'UNPLAN' end as GBN,
			A.OPERATION_QTY,
			case when A.END_TIME <= B.DUE_DATE then A.OPERATION_QTY 
				 else 0 end as ON_TIME_QTY,
			case when A.END_TIME > B.DUE_DATE then A.OPERATION_QTY 
				 else 0 end as LATE_QTY
	from	th_out_order_tracking A with (nolock)  -- select * from  th_out_order_tracking with (nolock) where  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' and out_version_id = 'SIMM_20250812_P01' and site_id = 'V_O' and ORDER_ID_TYPE = 'W' 
			left outer join 
			th_mst_demand B  with (NOLOCK) -- select * from  th_mst_demand with (nolock) where  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' 
			on A.master_id = B.master_id
			and A.plan_id = B.plan_id
			and A.DEMAND_ID = B.demand_id 
	where 1=1
	and A.master_id = 'SIMMTECH' 
	and A.plan_id = {terms["PLAN_ID"].V} 
	and A.out_version_id = {terms["PLAN_ID"].V}
	and A.site_id = 'V_O' 
	and A.ORDER_ID_TYPE = 'W'
	and B.CORPORATION = 'STK'
	and B.PLANT = 'STK'
	--order by A.demand_id
");
			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
	and B.DMD_ATTB_8 = {terms["group_id"].V}
");
			}

			sSQL.Append($@"
),
OTD_QTY as (
	select	max(DIVISION_ID) as DIVISION_ID,
			sum(OPERATION_QTY) as TOTAL_QTY, 
			sum(ON_TIME_QTY)   as ON_TIME_QTY, 
			sum(LATE_QTY) as LATE_QTY 
	from	OTD_LIST 
)
select	
		DIVISION_ID,
		'계획' AS TYPE1,
		'고객만족' AS TYPE2,
		'납기준수율(%)' AS TYPE3,
		TOTAL_QTY,
		ON_TIME_QTY, 
		case when TOTAL_QTY > 0 then ON_TIME_QTY/TOTAL_QTY*100 else 0 end as OTD_RATE 		
from  OTD_QTY 
");

			//Console.WriteLine(sSQL.ToString());

			return Data.Get(sSQL.ToString()).Tables[0];
        }



        private DataTable searchProdTarget(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
--===================================
-- 생산목표
--===================================
with 
PST as (
	select	master_id, plan_id, PLAN_START_DTTM, PLAN_START_DTTM+1 as TO_DTTM,
			dateadd(month, 1, dateadd(day, 1-DAY(PLAN_START_DTTM), PLAN_START_DTTM)) as NEXT_MONTH_1ST_PST,  --다음달 1일 시업시간 
			dateadd(month, 2, dateadd(day, 1-DAY(PLAN_START_DTTM), PLAN_START_DTTM)) as NEXT_2MONTH_1ST_PST  --다다음달 1일 시업시간 
	from th_eng_plan_info with (NOLOCK) 
	where master_id = 'SIMMTECH' 
	and plan_id = {terms["PLAN_ID"].V}  -- UI에서 생산계획 버전 선택 필요 ###########
	--order by plan_id desc
)
, 
OUT_PLAN_LIST as (
	select	--A.*, B.*, 
			D.DIVISION_ID,
			B.IRS_ATTB_2 as item_code, B.IRS_ATTB_3 as revision, A.END_TIME, A.OPERATION_QTY,
			C.NEXT_MONTH_1ST_PST, C.NEXT_2MONTH_1ST_PST,
			case when A.END_TIME < C.NEXT_MONTH_1ST_PST then A.OPERATION_QTY else 0 end  as this_month_plan_out_qty,
			case when A.END_TIME >= C.NEXT_MONTH_1ST_PST and A.END_TIME < C.NEXT_2MONTH_1ST_PST then A.OPERATION_QTY else 0 end as next_month_plan_out_qty
	from	th_out_work_order A with (nolock)  -- select * from  th_out_work_order with (nolock) where  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' and out_version_id = 'SIMM_20250812_P01' and site_id = 'V_O'  
			left outer join 
			th_mst_item_route_site B  with (NOLOCK) -- select * from  th_mst_item_route_site with (nolock) where corporation = 'STK' and plant = 'STK' and  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' 
			on  A.master_id = B.master_id
			and A.plan_id = B.plan_id
			and A.item_ID = B.item_id 
			left outer join 
			PST C 
			on  A.master_id = C.master_id
			and A.plan_id = C.plan_id
			and A.out_version_id = C.plan_id
			left outer join
			TH_GUI_ITEM_MODEL_SEARCH D with (nolock)
			on B.IRS_ATTB_2 = D.ITEM_CODE
	where 1=1
	and A.master_id = 'SIMMTECH' 
	and A.plan_id = {terms["PLAN_ID"].V} 
	and A.out_version_id = {terms["PLAN_ID"].V}
	and A.site_id = 'V_O' 
	and B.CORPORATION = 'STK'
	and B.PLANT = 'STK'
	--order by A.demand_id
");

			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
	and D.DIVISION_ID = {terms["group_id"].V}
");
			}

			sSQL.Append($@"
)
--select * from  OUT_PLAN_LIST ;
-- 하단 상세 : Pattern type, Layer별로 당월, 차월 완성 계획 물량 집계  
-- Pattern type, Layer 가져와서 붙일 것 (엑셀 업로드 Item master) 
,
PLANO_OUT_QTY as (
	select	MAX(DIVISION_ID) AS DIVISION_ID,
			sum(this_month_plan_out_qty) as THIS_MONTH_PLAN_OUT_TOTAL_QTY, 
			sum(next_month_plan_out_qty) as NEXT_MONTH_PLAN_OUT_TOTAL_QTY
	from	OUT_PLAN_LIST 
) 
--select * from PLANO_OUT_QTY;
SELECT DIVISION_ID, '계획' AS TYPE1, '생산목표(SQM)' AS TYPE2, '당월 예상완료 물량' AS TYPE3, THIS_MONTH_PLAN_OUT_TOTAL_QTY AS OTD_RATE
FROM PLANO_OUT_QTY
UNION ALL
SELECT DIVISION_ID, '계획' AS TYPE1, '생산목표(SQM)' AS TYPE2, '차월 예상완료 물량' AS TYPE3, NEXT_MONTH_PLAN_OUT_TOTAL_QTY AS OTD_RATE
FROM PLANO_OUT_QTY
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable searchProdTargetDetail(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
--===================================
-- 생산목표
--===================================
with 
PST as (
	select	master_id, plan_id, PLAN_START_DTTM, PLAN_START_DTTM+1 as TO_DTTM,
			dateadd(month, 1, dateadd(day, 1-DAY(PLAN_START_DTTM), PLAN_START_DTTM)) as NEXT_MONTH_1ST_PST,  --다음달 1일 시업시간 
			dateadd(month, 2, dateadd(day, 1-DAY(PLAN_START_DTTM), PLAN_START_DTTM)) as NEXT_2MONTH_1ST_PST  --다다음달 1일 시업시간 
	from th_eng_plan_info with (NOLOCK) 
	where master_id = 'SIMMTECH' 
	and plan_id = {terms["PLAN_ID"].V}  -- UI에서 생산계획 버전 선택 필요 ###########
	--order by plan_id desc
)
, 
OUT_PLAN_LIST as (
	select	--A.*, B.*, 
			D.DIVISION_ID,
			B.IRS_ATTB_2 as item_code, B.IRS_ATTB_3 as revision, A.END_TIME, A.OPERATION_QTY,
			C.NEXT_MONTH_1ST_PST, C.NEXT_2MONTH_1ST_PST,
			case when A.END_TIME < C.NEXT_MONTH_1ST_PST then A.OPERATION_QTY else 0 end  as this_month_plan_out_qty,
			case when A.END_TIME >= C.NEXT_MONTH_1ST_PST and A.END_TIME < C.NEXT_2MONTH_1ST_PST then A.OPERATION_QTY else 0 end as next_month_plan_out_qty
	from	th_out_work_order A with (nolock)  -- select * from  th_out_work_order with (nolock) where  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' and out_version_id = 'SIMM_20250812_P01' and site_id = 'V_O'  
			left outer join 
			th_mst_item_route_site B  with (NOLOCK) -- select * from  th_mst_item_route_site with (nolock) where corporation = 'STK' and plant = 'STK' and  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' 
			on  A.master_id = B.master_id
			and A.plan_id = B.plan_id
			and A.item_ID = B.item_id 
			left outer join 
			PST C 
			on  A.master_id = C.master_id
			and A.plan_id = C.plan_id
			and A.out_version_id = C.plan_id
			left outer join
			TH_GUI_ITEM_MODEL_SEARCH D with (nolock)
			on B.IRS_ATTB_2 = D.ITEM_CODE
	where 1=1
	and A.master_id = 'SIMMTECH' 
	and A.plan_id = {terms["PLAN_ID"].V} 
	and A.out_version_id = {terms["PLAN_ID"].V}
	and A.site_id = 'V_O' 
	and B.CORPORATION = 'STK'
	and B.PLANT = 'STK'
	--order by A.demand_id
");

			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
	and D.DIVISION_ID = {terms["group_id"].V}
");
			}

			sSQL.Append($@"
) 
--select * from OUT_PLAN_LIST;
select 
	C.CATEGORY_LEVEL1,
	B.D_LAYER,
	sum(A.this_month_plan_out_qty) AS THIS_MONTH,
	sum(A.next_month_plan_out_qty) AS NEXT_MONTH
from  
	OUT_PLAN_LIST A
	LEFT JOIN TH_GUI_ITEM_BY_PROCESS_GUBUN B
	ON A.ITEM_CODE = B.ITEM_CODE 
	LEFT JOIN (
		select 
			SEGMENT2 AS GROUP_ID,
			SEGMENT3 AS CATEGORY_LEVEL1,
			SEGMENT4 AS PATTERN_GUBUN
		from 
			[dbo].[LOOKUP_VALUE_M]
		WHERE 
			1=1
			AND LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY'
			AND LOOKUP_TYPE_VERSION = ( select MAX(LOOKUP_TYPE_VERSION)
										FROM [dbo].[LOOKUP_VALUE_M]
										WHERE LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY')
			AND SEGMENT2 = 'SPS'
	) C
	ON B.PATTERN_GUBUN = C.PATTERN_GUBUN
group by CATEGORY_LEVEL1, B.D_LAYER
order by CATEGORY_LEVEL1, CAST(B.D_LAYER AS INT)
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }


        private DataTable searchLotTAT(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
--===================================
-- 신규 Lot별 TAT 
--===================================
with 
DEMAND_LIST as 
(
	select master_id,plan_id, DEMAND_ID, DMD_ATTB_2 as item_code, DMD_ATTB_3 as REVISION, DMD_ATTB_8 AS DIVISION
	from th_mst_demand A  with (NOLOCK) -- select * from  th_mst_demand with (nolock) where  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' 			
	where 1=1
	and A.CORPORATION = 'STK'
	and A.PLANT = 'STK'
	and A.master_id = 'SIMMTECH' 
	and A.plan_id = {terms["PLAN_ID"].V} 
	and A.DMD_ATTB_1 = 'NEW'
	--group by demand_id, DMD_ATTB_2, DMD_ATTB_3
	--order by A.demand_id
)
,
APS_NEW_TAT as (
	select  A.DIVISION, A.DEMAND_ID, A.ITEM_CODE, A.REVISION, --B.*  
			B.route_id, B.site_id, B.START_TIME, B.END_TIME, B.OPERATION_QTY
	from	DEMAND_LIST A
			left outer join 
			TH_OUT_ORDER_TRACKING B  -- select top 10 * from TH_OUT_ORDER_TRACKING A where  A.master_id = 'SIMMTECH' and A.plan_id = 'SIMM_20250812_P01' and A.out_version_id = 'SIMM_20250812_P01' and A.site_id in ('V_I','V_O') and demand_id = '20250812_00002' and A.ORDER_ID_TYPE = 'W' 
			on  A.master_id= B.master_id 
			and A.plan_id = B.plan_id 
			and A.plan_id = B.out_Version_id 
			and A.DEMAND_ID = B.DEMAND_ID
	where 1=1
	and B.site_id in ('V_I','V_O') 
	and B.ORDER_ID_TYPE = 'W' 
), 
NEW_LOT_START_END as (
	select DIVISION, DEMAND_ID, ITEM_CODE, REVISION,
			 min(START_TIME) as lot_start_time, max(END_TIME) as lot_end_Time
	from APS_NEW_TAT 
	where 1=1
");

            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
		and DIVISION = {terms["group_id"].V}
");
            }

            sSQL.Append($@"
	group by  DIVISION, DEMAND_ID, ITEM_CODE, REVISION
	--order by demand_id
)
,
CATEGORY_TAT AS (
    SELECT
		A.DIVISION AS DIVISION_ID,
        C.CATEGORY_LEVEL1,
        ROUND(DATEDIFF(MINUTE, A.lot_start_time, A.lot_end_Time) / 60.0 / 24.0, 2) AS TAT
    FROM NEW_LOT_START_END A
    LEFT JOIN TH_GUI_ITEM_BY_PROCESS_GUBUN B
        ON A.ITEM_CODE = B.ITEM_CODE
    LEFT JOIN (
        SELECT 
            SEGMENT2 AS GROUP_ID,
            SEGMENT3 AS CATEGORY_LEVEL1,
            SEGMENT4 AS PATTERN_GUBUN
        FROM [dbo].[LOOKUP_VALUE_M]
        WHERE LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY'
          AND LOOKUP_TYPE_VERSION = (
              SELECT MAX(LOOKUP_TYPE_VERSION)
              FROM [dbo].[LOOKUP_VALUE_M]
              WHERE LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY'
          )
    ) C ON B.PATTERN_GUBUN = C.PATTERN_GUBUN
       AND A.DIVISION = C.GROUP_ID
    WHERE A.lot_start_time IS NOT NULL 
      AND A.lot_end_Time IS NOT NULL
)
SELECT 
	MAX(DIVISION_ID) AS DIVISION_ID,
    '계획' AS TYPE1,
	'Lead Time(day)' AS TYPE2,
    CATEGORY_LEVEL1 AS TYPE3,
    ROUND(AVG(TAT), 2) AS OTD_RATE
FROM CATEGORY_TAT
GROUP BY CATEGORY_LEVEL1
ORDER BY CATEGORY_LEVEL1;
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable searchLotTATDetail(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

			sSQL.Append($@"
--===================================
-- 신규 Lot별 TAT 
--===================================
with 
DEMAND_LIST as 
(
	select master_id,plan_id, DEMAND_ID, DMD_ATTB_2 as item_code, DMD_ATTB_3 as REVISION, DMD_ATTB_8 AS DIVISION
	from th_mst_demand A  with (NOLOCK) -- select * from  th_mst_demand with (nolock) where  master_id = 'SIMMTECH' and plan_id = 'SIMM_20250812_P01' 			
	where 1=1
	and A.CORPORATION = 'STK'
	and A.PLANT = 'STK'
	and A.master_id = 'SIMMTECH' 
	and A.plan_id = {terms["PLAN_ID"].V} 
	and A.DMD_ATTB_1 = 'NEW'  --## NEW로 변경 예정 
	--group by demand_id, DMD_ATTB_2, DMD_ATTB_3
	--order by A.demand_id	
)
,
APS_NEW_TAT as (
	select  A.DIVISION, A.DEMAND_ID, A.ITEM_CODE, A.REVISION, --B.*  
			B.route_id, B.site_id, B.START_TIME, B.END_TIME, B.OPERATION_QTY
	from	DEMAND_LIST A
			left outer join 
			TH_OUT_ORDER_TRACKING B  -- select top 10 * from TH_OUT_ORDER_TRACKING A where  A.master_id = 'SIMMTECH' and A.plan_id = 'SIMM_20250812_P01' and A.out_version_id = 'SIMM_20250812_P01' and A.site_id in ('V_I','V_O') and demand_id = '20250812_00002' and A.ORDER_ID_TYPE = 'W' 
			on  A.master_id= B.master_id 
			and A.plan_id = B.plan_id 
			and A.plan_id = B.out_Version_id 
			and A.DEMAND_ID = B.DEMAND_ID
	where 1=1
	and B.site_id in ('V_I','V_O') 
	and B.ORDER_ID_TYPE = 'W' 
), 
NEW_LOT_START_END as (
	select DIVISION, DEMAND_ID, ITEM_CODE, REVISION,
			 min(START_TIME) as lot_start_time, max(END_TIME) as lot_end_Time
	from APS_NEW_TAT 
	where 1=1
");
			if (terms["group_id"].Length > 0)
			{
				sSQL.Append($@"
		and DIVISION = {terms["group_id"].V}
");
			}


			sSQL.Append($@"
	group by  DIVISION, DEMAND_ID, ITEM_CODE, REVISION
	--order by demand_id
)
SELECT
    C.CATEGORY_LEVEL1,
    B.D_LAYER,
    A.ITEM_CODE,
    COUNT(DISTINCT A.DEMAND_ID) AS DEMAND_COUNT,
    ROUND(AVG(TAT), 2) AS AVG_TAT,
    ROUND(MAX(TAT), 2) AS MAX_TAT,
    ROUND(MIN(TAT), 2) AS MIN_TAT,
    ROUND(STDEV(TAT), 2) AS STD_TAT
FROM (
    SELECT 
        ITEM_CODE,
        DEMAND_ID,
        lot_start_time,
        lot_end_time,
        DATEDIFF(MINUTE, lot_start_time, lot_end_time) / 60.0 / 24.0 AS TAT,
        DIVISION
    FROM NEW_LOT_START_END
    WHERE lot_start_time IS NOT NULL AND lot_end_time IS NOT NULL
) A
LEFT JOIN TH_GUI_ITEM_BY_PROCESS_GUBUN B
    ON A.ITEM_CODE = B.ITEM_CODE
LEFT JOIN (
    SELECT 
        SEGMENT2 AS GROUP_ID,
        SEGMENT3 AS CATEGORY_LEVEL1,
        SEGMENT4 AS PATTERN_GUBUN
    FROM [dbo].[LOOKUP_VALUE_M]
    WHERE LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY'
      AND LOOKUP_TYPE_VERSION = (
          SELECT MAX(LOOKUP_TYPE_VERSION)
          FROM [dbo].[LOOKUP_VALUE_M]
          WHERE LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY'
      )
) C ON B.PATTERN_GUBUN = C.PATTERN_GUBUN
   AND A.DIVISION = C.GROUP_ID
GROUP BY C.CATEGORY_LEVEL1, B.D_LAYER, A.ITEM_CODE
ORDER BY C.CATEGORY_LEVEL1, CAST(B.D_LAYER AS INT), A.ITEM_CODE
");
			Console.WriteLine(sSQL.ToString());

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


    }
}
