using DocumentFormat.OpenXml.Spreadsheet;
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
    public class process_item_simul : BasePageModel
    {

        public process_item_simul()
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
--만들쿼리 

DROP TABLE IF EXISTS #TEMP_TABLE;
with 
WIP_SIMUL_LOOKUP as ( 
	select segment1 as WRG_ID, segment2 as RCG_ID, SEGMENT3 as RESOURCE_LEVEL, SEGMENT4 as LEVEL2_DEFINITION_LOOKUP
	from lookup_value_m with (nolock)
	where  LOOKUP_TYPE_CODE = 'WIP_SIMULATION_CLASSIFICATION' 
	and lookup_type_version = (select lookup_type_version from lookup_type_m with (nolock) where  LOOKUP_TYPE_CODE = 'WIP_SIMULATION_CLASSIFICATION' and active_flag = 'Y' ) 
	and active_flag = 'Y' 
	-- order by 3,1,2
),
WIP_SIMUL_LIST as (
	select	B.RESOURCE_LEVEL, A.DIVISION_ID,  A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.DEPT_CODE 
	from	TH_TAR_DEPT_MASTER_WITH_NAME_V A
			inner join 
			WIP_SIMUL_LOOKUP B with (nolock)
			on A.APS_WIP_ROUTE_GRP_ID = B.WRG_ID
			and	A.RESOURCE_CAPA_GROUP_ID = B.RCG_ID 
			and	B.resource_level = 1 
                   ");
			 if (terms["group_id"].Length > 0)
             {
                sSQL.Append($@"
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
             }
            sSQL.Append($@"		
                			
) 
--select * from WIP_SIMUL_LIST A
--order by A.RESOURCE_LEVEL, A.APS_WIP_ROUTE_GRP_ID, A.RESOURCE_CAPA_GROUP_ID
, 
INCOMING_RST_DETAIL as (
	select	--  A.*, b.item_id  -- , B.*, C.* 
			C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE,
			--B.IRS_ATTB_5 as DEPT_CODE, 
			B.IRS_ATTB_12 as LAYER_INOUT, 
			B.IRS_ATTB_11 as WIP_GBN, 
			B.ROUTE_ID, B.ROUTE_LEVEL, -- 참고용 
			A.OPERATION_QTY, 
			A.OUT_ITEM_ID, -- BOD Item  --## 다름 
			b.item_id, -- 참조용  
			-- A.END_TIME,
			CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS BASE_DATE -- BASE_END_DATE
			-- b.* , A.*		
			---- A.END_TIME -- BOD 입고 완료시간 
			--CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS DELIVERY_IN_DATE
	from	TH_OUT_ORDER_TRACKING  A with (nolock)  
			inner join 
			TH_MST_ITEM_ROUTE_SITE B with (nolock)  
			on B.CORPORATION = 'STK' 
			and B.PLANT = 'STK' 
			and A.master_id = B.master_id 
			and A.plan_id = B.plan_id
			and a.to_site_id = b.site_id	-- TO  같은 것  - site는 집계는 자사 외주 나누지 않음
			and a.to_route_id = b.route_id	-- TO  같은 것 
			--and a.to_operation_id = b.operation_id	-- TO  같은 것 
			and SUBSTRING(A.OUT_ITEM_ID, 1, CHARINDEX('__', A.OUT_ITEM_ID) - 1)  = SUBSTRING(B.item_id , 1, CHARINDEX('__', B.item_id ) - 1) 
			inner join 
			WIP_SIMUL_LIST C with (nolock) 
			on B.IRS_ATTB_5 = C.DEPT_CODE
			and B.IRS_ATTB_8 = C.RESOURCE_CAPA_GROUP_ID
			--and B.IRS_ATTB_15 = C.APS_WIP_ROUTE_GRP_ID  --####  다음 버전부터 나옴 나중에 풀 것. 
	where A.master_id = 'SIMMTECH' 
	and A.PLAN_ID = '{terms["PLAN_ID"].AsString()}' 
	and A.OUT_VERSION_ID =  '{terms["PLAN_ID"].AsString()}'
	and A.ORDER_ID_TYPE ='D'		
	and C.RESOURCE_LEVEL = 1 -- 1레벨만 우선. 2레벨은 추가 설정. 
	
) 
--select * from INCOMING_RST_DETAIL A
, 
INCOMING_RST_LEVEL1_SUM as (
	select 'INCOMING' as GBN, A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, 
		   A.BASE_DATE, sum(operation_qty) as TOT_QTY  
	from INCOMING_RST_DETAIL  A with (nolock)
	group by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date 
	
),
OUTGOING_RST_DETAIL as (
	select	--  A.*, b.item_id  -- , B.*, C.* 
			C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE,
			--B.IRS_ATTB_5 as DEPT_CODE, 
			B.IRS_ATTB_12 as LAYER_INOUT, 
			B.IRS_ATTB_11 as WIP_GBN, 
			B.ROUTE_ID, B.ROUTE_LEVEL, -- 참고용 
			A.OPERATION_QTY, 
			--A.OUT_ITEM_ID, -- BOD Item  
			--A.ROUTE_ID as OT_ROUTE_ID, -- 참고용 임시 
			A.IN_ITEM_ID, -- BOD Item    --## 다름 
			b.item_id, -- 참조용  
			-- A.END_TIME,
			CONVERT(DATE, DATEADD(MINUTE, -450, A.START_TIME)) AS BASE_DATE -- BASE_START_DATE
			-- b.* , A.*		
			---- A.END_TIME -- BOD 입고 완료시간 
			--CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS DELIVERY_IN_DATE
	from	TH_OUT_ORDER_TRACKING A with (nolock)  
			inner join 
			TH_MST_ITEM_ROUTE_SITE B with (nolock)  
			on B.CORPORATION = 'STK' 
			and B.PLANT = 'STK' 
			and A.master_id = B.master_id 
			and A.plan_id = B.plan_id
			and a.from_site_id = b.site_id	-- TO  같은 것  - site는 집계는 자사 외주 나누지 않음
			and a.from_route_id = b.route_id	-- TO  같은 것 			
			and A.IN_ITEM_ID = B.ITEM_ID --## 다름. OUTGOING은 바로 걸어도 됨 
			inner join 
			WIP_SIMUL_LIST C 
			on B.IRS_ATTB_5 = C.DEPT_CODE
			and B.IRS_ATTB_8 = C.RESOURCE_CAPA_GROUP_ID			
	where A.master_id = 'SIMMTECH' and A.PLAN_ID = '{terms["PLAN_ID"].AsString()}'   and A.OUT_VERSION_ID = '{terms["PLAN_ID"].AsString()}'  and A.ORDER_ID_TYPE ='D'
	
	and C.RESOURCE_LEVEL = 1 -- 1레벨만 우선. 2레벨은 추가 설정. 
) 
--select * from OUTGOING_RST_DETAIL A
--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.DEPT_CODE -- , A.LAYER_INOUT, A.Base_date
--;
,
OUTGOING_RST_LEVEL1_SUM as (
	select 'OUTGOING' as GBN, A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, 
		   A.Base_date, sum(operation_qty) as TOT_QTY  -- A.DEPT_CODE, 
	from OUTGOING_RST_DETAIL A with (nolock)
	group by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
	--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
)


SELECT   A.*,ROUND(BOHTB.BOH,1) as FIRST_BOH INTO #TEMP_TABLE
FROM (
	select * from INCOMING_RST_LEVEL1_SUM  with (nolock) 
	union all 
	select * from OUTGOING_RST_LEVEL1_SUM  with (nolock) 
) A 

LEFT OUTER JOIN 
(	
	SELECT
		A.DIVISION_ID, 
		B.APS_WIP_ROUTE_GRP_ID, 
		B.WIP_ROUTE_GROUP_NAME, 
		B.RESOURCE_CAPA_GROUP_ID, 
		B.RESOURCE_CAPA_GROUP_NAME,
		CASE 
			WHEN A.INNER_OUTER = 'I' THEN 'INNER' 
			WHEN A.INNER_OUTER = 'O' THEN 'OUTER' 
			ELSE ''  
		END AS LAYEER_INOUT, 
		SUM(A.SQM) AS BOH
	FROM (
		SELECT A.*
		FROM th_tar_wip_his A WITH (NOLOCK)
		INNER JOIN th_mst_plan B WITH (NOLOCK)
			ON A.YYYYMMDD = B.PLAN_ATTB_1
			AND A.SEQ = B.PLAN_ATTB_2
		WHERE 1=1
			");
					if (terms["group_id"].Length > 0)
					{
					sSQL.Append($@"
						AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
					");
					}
					sSQL.Append($@"	
			AND A.DIVISION_ID = 'SPS'
			AND B.CORPORATION = 'STK'
			AND B.PLANT = 'STK'
			AND B.master_id = 'SIMMTECH'
			AND B.plan_id = '{terms["PLAN_ID"].AsString()}'
	) A
	INNER JOIN TH_TAR_DEPT_MASTER_WITH_NAME_V B WITH (NOLOCK)
		ON A.DIVISION_ID = B.DIVISION_ID
		AND A.DEPT_CODE = B.DEPT_CODE
	LEFT OUTER JOIN WIP_ROUTE_GROUP_V3 C
		ON B.APS_WIP_ROUTE_GRP_ID = C.APS_WIP_ROUTE_GRP_ID
	WHERE 1=1 --B.WIP_ROUTE_GROUP_NAME = 'S/M'
	GROUP BY 
		A.DIVISION_ID, 
		B.APS_WIP_ROUTE_GRP_ID, 
		B.WIP_ROUTE_GROUP_NAME, 
		A.INNER_OUTER, 
		C.sort_order, 
		B.RESOURCE_CAPA_GROUP_ID, 
		B.RESOURCE_CAPA_GROUP_NAME    
	)
	AS BOHTB 
		ON A.APS_WIP_ROUTE_GRP_ID = BOHTB.APS_WIP_ROUTE_GRP_ID AND A.LAYER_INOUT = BOHTB.LAYEER_INOUT AND A.RESOURCE_CAPA_GROUP_ID = BOHTB.RESOURCE_CAPA_GROUP_ID
 where 1=1
 --and A.APS_WIP_ROUTE_GRP_ID = 'APS_WRG_0018'
 --and base_date = '2025-09-29'
;



SELECT *
FROM 
(
	
	SELECT 		
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		'NEW+WIP' WIP_GBN,	
		'BOH' GBN,
		RESOURCE_LEVEL,
		1 V_LEVEL,
		BASE_DATE,	
		0 as FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A  with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' AND '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, FIRST_BOH,A.WIP_GBN, A.BASE_DATE 
	UNION ALL 
	SELECT 		
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		'NEW+WIP' WIP_GBN,	
		'EOH' GBN,
		RESOURCE_LEVEL,
		2 V_LEVEL,
		BASE_DATE,	
		0 as FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A  with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' AND '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, FIRST_BOH,A.WIP_GBN, A.BASE_DATE 
	UNION ALL 	
	SELECT 		
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		WIP_GBN,	
		'BOH' GBN,
		RESOURCE_LEVEL,
		(CASE WHEN WIP_GBN ='WIP' THEN 3
              WHEN WIP_GBN ='NEW' THEN 6 END
        ) AS V_LEVEL,
		BASE_DATE,	
		FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A  with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' AND '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, FIRST_BOH,A.WIP_GBN, A.BASE_DATE 
	UNION ALL 
	SELECT 	
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		WIP_GBN,	
		GBN,
		RESOURCE_LEVEL,
		(CASE WHEN WIP_GBN ='WIP' THEN 4
              WHEN WIP_GBN ='NEW' THEN 7 END
        ) AS V_LEVEL,
		BASE_DATE,	
		FIRST_BOH,
		TOT_QTY	
	FROM #TEMP_TABLE A with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' AND '{endDateStr}' 
	UNION ALL 
	SELECT 
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		WIP_GBN,	
		'EOH' GBN,
		RESOURCE_LEVEL,
		(CASE WHEN WIP_GBN ='WIP' THEN 5
              WHEN WIP_GBN ='NEW' THEN 8 END
        ) AS V_LEVEL,
		BASE_DATE,	
		FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A with (nolock) 
	WHERE BASE_DATE BETWEEN '{startDateStr}' AND '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN,FIRST_BOH, A.BASE_DATE 

) AS SourceTable
PIVOT (
   SUM(TOT_QTY)
   FOR BASE_DATE IN (
					{result}
					)
) AS PivotResult
order by  RESOURCE_LEVEL,DIVISION_ID, WIP_ROUTE_GROUP_NAME,   RESOURCE_CAPA_GROUP_NAME ,LAYER_INOUT, V_LEVEL,WIP_GBN,GBN
;



");

			Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }




        private DataTable SearchBasic_백업250929(Params terms)
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



DROP TABLE IF EXISTS #TEMP_TABLE;
with 
WIP_SIMUL_LOOKUP as ( 
	select segment1 as WRG_ID, segment2 as RCG_ID, SEGMENT3 as RESOURCE_LEVEL, SEGMENT4 as LEVEL2_DEFINITION_LOOKUP
	from lookup_value_m with (nolock)
	where  LOOKUP_TYPE_CODE = 'WIP_SIMULATION_CLASSIFICATION' 
	and lookup_type_version = (select lookup_type_version from lookup_type_m with (nolock) where  LOOKUP_TYPE_CODE = 'WIP_SIMULATION_CLASSIFICATION' and active_flag = 'Y' ) 
	and active_flag = 'Y' 
	-- order by 3,1,2
),
WIP_SIMUL_LIST as (
	select	B.RESOURCE_LEVEL, A.DIVISION_ID,  A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.DEPT_CODE 
	from	TH_TAR_DEPT_MASTER_WITH_NAME_V A
			inner join 
			WIP_SIMUL_LOOKUP B with (nolock)
			on A.APS_WIP_ROUTE_GRP_ID = B.WRG_ID
			and	A.RESOURCE_CAPA_GROUP_ID = B.RCG_ID 
			and	B.resource_level = 1 ");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
            }
            sSQL.Append($@"			
) 
--select * from WIP_SIMUL_LIST A
--order by A.RESOURCE_LEVEL, A.APS_WIP_ROUTE_GRP_ID, A.RESOURCE_CAPA_GROUP_ID
, 
INCOMING_RST_DETAIL as (
	select	--  A.*, b.item_id  -- , B.*, C.* 
			C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE,
			--B.IRS_ATTB_5 as DEPT_CODE, 
			B.IRS_ATTB_12 as LAYER_INOUT, 
			B.IRS_ATTB_11 as WIP_GBN, 
			B.ROUTE_ID, B.ROUTE_LEVEL, -- 참고용 
			A.OPERATION_QTY, 
			A.OUT_ITEM_ID, -- BOD Item  --## 다름 
			b.item_id, -- 참조용  
			-- A.END_TIME,
			CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS BASE_DATE -- BASE_END_DATE
			-- b.* , A.*		
			---- A.END_TIME -- BOD 입고 완료시간 
			--CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS DELIVERY_IN_DATE
	from	TH_OUT_ORDER_TRACKING  A with (nolock)  
			inner join 
			TH_MST_ITEM_ROUTE_SITE B with (nolock)  
			on B.CORPORATION = 'STK' 
			and B.PLANT = 'STK' 
			and A.master_id = B.master_id 
			and A.plan_id = B.plan_id
			and a.to_site_id = b.site_id	-- TO  같은 것  - site는 집계는 자사 외주 나누지 않음
			and a.to_route_id = b.route_id	-- TO  같은 것 
			--and a.to_operation_id = b.operation_id	-- TO  같은 것 
			and SUBSTRING(A.OUT_ITEM_ID, 1, CHARINDEX('__', A.OUT_ITEM_ID) - 1)  = SUBSTRING(B.item_id , 1, CHARINDEX('__', B.item_id ) - 1) 
			inner join 
			WIP_SIMUL_LIST C with (nolock) 
			on B.IRS_ATTB_5 = C.DEPT_CODE
			and B.IRS_ATTB_8 = C.RESOURCE_CAPA_GROUP_ID
			--and B.IRS_ATTB_15 = C.APS_WIP_ROUTE_GRP_ID  --####  다음 버전부터 나옴 나중에 풀 것. 
	where A.master_id = 'SIMMTECH' 
	and A.PLAN_ID = '{terms["PLAN_ID"].AsString()}' 
	and A.OUT_VERSION_ID = '{terms["PLAN_ID"].AsString()}' 
	and A.ORDER_ID_TYPE ='D'		
	and C.RESOURCE_LEVEL = 1 -- 1레벨만 우선. 2레벨은 추가 설정. 
	
) 
--select * from INCOMING_RST_DETAIL A
, 
INCOMING_RST_LEVEL1_SUM as (
	select 'INCOMING' as GBN, A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, 
		   A.BASE_DATE, sum(operation_qty) as TOT_QTY  
	from INCOMING_RST_DETAIL  A with (nolock)
	group by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date 
	
),
OUTGOING_RST_DETAIL as (
	select	--  A.*, b.item_id  -- , B.*, C.* 
			C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE,
			--B.IRS_ATTB_5 as DEPT_CODE, 
			B.IRS_ATTB_12 as LAYER_INOUT, 
			B.IRS_ATTB_11 as WIP_GBN, 
			B.ROUTE_ID, B.ROUTE_LEVEL, -- 참고용 
			A.OPERATION_QTY, 
			--A.OUT_ITEM_ID, -- BOD Item  
			--A.ROUTE_ID as OT_ROUTE_ID, -- 참고용 임시 
			A.IN_ITEM_ID, -- BOD Item    --## 다름 
			b.item_id, -- 참조용  
			-- A.END_TIME,
			CONVERT(DATE, DATEADD(MINUTE, -450, A.START_TIME)) AS BASE_DATE -- BASE_START_DATE
			-- b.* , A.*		
			---- A.END_TIME -- BOD 입고 완료시간 
			--CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS DELIVERY_IN_DATE
	from	TH_OUT_ORDER_TRACKING A with (nolock)  
			inner join 
			TH_MST_ITEM_ROUTE_SITE B with (nolock)  
			on B.CORPORATION = 'STK' 
			and B.PLANT = 'STK' 
			and A.master_id = B.master_id 
			and A.plan_id = B.plan_id
			and a.from_site_id = b.site_id	-- TO  같은 것  - site는 집계는 자사 외주 나누지 않음
			and a.from_route_id = b.route_id	-- TO  같은 것 			
			and A.IN_ITEM_ID = B.ITEM_ID --## 다름. OUTGOING은 바로 걸어도 됨 
			inner join 
			WIP_SIMUL_LIST C 
			on B.IRS_ATTB_5 = C.DEPT_CODE
			and B.IRS_ATTB_8 = C.RESOURCE_CAPA_GROUP_ID			
	where A.master_id = 'SIMMTECH' and A.PLAN_ID = '{terms["PLAN_ID"].AsString()}'   and A.OUT_VERSION_ID = '{terms["PLAN_ID"].AsString()}'  and A.ORDER_ID_TYPE ='D'
	
	and C.RESOURCE_LEVEL = 1 -- 1레벨만 우선. 2레벨은 추가 설정. 
) 
--select * from OUTGOING_RST_DETAIL A
--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.DEPT_CODE -- , A.LAYER_INOUT, A.Base_date
--;
,
OUTGOING_RST_LEVEL1_SUM as (
	select 'OUTGOING' as GBN, A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, 
		   A.Base_date, sum(operation_qty) as TOT_QTY  -- A.DEPT_CODE, 
	from OUTGOING_RST_DETAIL A with (nolock)
	group by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
	--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
)
SELECT A.*,ROUND(BOHTB.BOH,1) as FIRST_BOH INTO #TEMP_TABLE
FROM (
	select * from INCOMING_RST_LEVEL1_SUM  with (nolock) 
	union all 
	select * from OUTGOING_RST_LEVEL1_SUM  with (nolock) 
) A 
LEFT OUTER JOIN 
(		
			SELECT
				A.DIVISION_ID,
				B.APS_WIP_ROUTE_GRP_ID,
				B.WIP_ROUTE_GROUP_NAME,
				CASE 
					WHEN A.INNER_OUTER = 'I' THEN 'INNER'
					WHEN A.INNER_OUTER = 'O' THEN 'OUTER'
					ELSE ''
				END AS LAYEER_INOUT,
				SUM(A.SQM) AS BOH
			FROM th_tar_wip A WITH (NOLOCK)
			INNER JOIN TH_TAR_DEPT_MASTER_WITH_NAME_V B WITH (NOLOCK)
				ON A.DIVISION_ID = B.DIVISION_ID
				AND A.DEPT_CODE = B.DEPT_CODE
			WHERE 1=1 
			");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
            }
            sSQL.Append($@"	
			GROUP BY 
				A.DIVISION_ID, 
				B.WIP_ROUTE_GROUP_NAME,
				B.APS_WIP_ROUTE_GRP_ID,
				A.INNER_OUTER
		) AS BOHTB ON A.APS_WIP_ROUTE_GRP_ID = BOHTB.APS_WIP_ROUTE_GRP_ID AND A.LAYER_INOUT = BOHTB.LAYEER_INOUT
where 1=1 
AND WIP_GBN IN ('WIP','NEW')
-- where A.RESOURCE_CAPA_GROUP_ID in('APS_RCG_0012','APS_RCG_0031')
-- and A.RESOURCE_CAPA_GROUP_ID in('APS_RCG_0012')
;


SELECT *
FROM 
(
	
	SELECT 		
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		'NEW+WIP' WIP_GBN,	
		'BOH' GBN,
		RESOURCE_LEVEL,
		1 V_LEVEL,
		BASE_DATE,	
		0 as FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A  with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' AND '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, FIRST_BOH,A.WIP_GBN, A.BASE_DATE 
	UNION ALL 
	SELECT 		
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		'NEW+WIP' WIP_GBN,	
		'EOH' GBN,
		RESOURCE_LEVEL,
		2 V_LEVEL,
		BASE_DATE,	
		0 as FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A  with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' AND '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, FIRST_BOH,A.WIP_GBN, A.BASE_DATE 
	UNION ALL 	
	SELECT 		
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		WIP_GBN,	
		'BOH' GBN,
		RESOURCE_LEVEL,
		3 V_LEVEL,
		BASE_DATE,	
		FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A  with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' and '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, FIRST_BOH,A.WIP_GBN, A.BASE_DATE 
	UNION ALL 
	SELECT 	
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		WIP_GBN,	
		GBN,
		RESOURCE_LEVEL,
		(CASE WHEN WIP_GBN ='WIP' THEN 4 
			  WHEN WIP_GBN ='NEW' THEN 5 END
		) AS V_LEVEL,
		BASE_DATE,	
		FIRST_BOH,
		TOT_QTY	
	FROM #TEMP_TABLE A with (nolock) 
	WHERE BASE_DATE BETWEEN   '{startDateStr}' and '{endDateStr}' 
	UNION ALL 
	SELECT 
		DIVISION_ID,
		APS_WIP_ROUTE_GRP_ID,
		WIP_ROUTE_GROUP_NAME,
		RESOURCE_CAPA_GROUP_ID,
		RESOURCE_CAPA_GROUP_NAME,
		LAYER_INOUT,
		WIP_GBN,	
		'EOH' GBN,
		RESOURCE_LEVEL,
		6 V_LEVEL,
		BASE_DATE,	
		FIRST_BOH,
		0 TOT_QTY
	FROM #TEMP_TABLE A with (nolock) 
	WHERE BASE_DATE BETWEEN '{startDateStr}' and '{endDateStr}' 
	GROUP BY RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN,FIRST_BOH, A.BASE_DATE 

) AS SourceTable
PIVOT (
   SUM(TOT_QTY)
   FOR BASE_DATE IN (
				{result}
					)
) AS PivotResult
order by  RESOURCE_LEVEL,DIVISION_ID, WIP_ROUTE_GROUP_NAME,   RESOURCE_CAPA_GROUP_NAME ,LAYER_INOUT, V_LEVEL,WIP_GBN,GBN
;

");

            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }



    }
}



/*




DROP TABLE IF EXISTS #TEMP_TABLE;
with 
WIP_SIMUL_LOOKUP as ( 
	select segment1 as WRG_ID, segment2 as RCG_ID, SEGMENT3 as RESOURCE_LEVEL, SEGMENT4 as LEVEL2_DEFINITION_LOOKUP
	from lookup_value_m with (nolock)
	where  LOOKUP_TYPE_CODE = 'WIP_SIMULATION_CLASSIFICATION' 
	and lookup_type_version = (select lookup_type_version from lookup_type_m with (nolock) where  LOOKUP_TYPE_CODE = 'WIP_SIMULATION_CLASSIFICATION' and active_flag = 'Y' ) 
	and active_flag = 'Y' 
	-- order by 3,1,2
),
WIP_SIMUL_LIST as (
	select	B.RESOURCE_LEVEL, A.DIVISION_ID,  A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.DEPT_CODE 
	from	TH_TAR_DEPT_MASTER_WITH_NAME_V A
			inner join 
			WIP_SIMUL_LOOKUP B with (nolock)
			on A.APS_WIP_ROUTE_GRP_ID = B.WRG_ID
			and	A.RESOURCE_CAPA_GROUP_ID = B.RCG_ID 
			and	B.resource_level = 1 
	--order by b.RESOURCE_LEVEL, b.WRG_ID, b.RCG_ID
) 
--select * from WIP_SIMUL_LIST A
--order by A.RESOURCE_LEVEL, A.APS_WIP_ROUTE_GRP_ID, A.RESOURCE_CAPA_GROUP_ID
, 
INCOMING_RST_DETAIL as (
	select	--  A.*, b.item_id  -- , B.*, C.* 
			C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE,
			--B.IRS_ATTB_5 as DEPT_CODE, 
			B.IRS_ATTB_12 as LAYER_INOUT, 
			B.IRS_ATTB_11 as WIP_GBN, 
			B.ROUTE_ID, B.ROUTE_LEVEL, -- 참고용 
			A.OPERATION_QTY, 
			A.OUT_ITEM_ID, -- BOD Item  --## 다름 
			b.item_id, -- 참조용  
			-- A.END_TIME,
			CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS BASE_DATE -- BASE_END_DATE
			-- b.* , A.*		
			---- A.END_TIME -- BOD 입고 완료시간 
			--CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS DELIVERY_IN_DATE
	from	TH_OUT_ORDER_TRACKING  A with (nolock)  -- select top 10 A.MASTER_ID, A.PLAN_ID, A.OUT_VERSION_ID, A.DEMAND_ID, A.TRACK_SEQ, A.OPERATION_SEQ, A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, A.RESOURCE_ID, A.OUT_ITEM_ID, A.COMPLETE_QTY, A.OUT_ITEM_PEGGING_ID, A.IN_ITEM_ID, A.OPERATION_QTY, A.IN_ITEM_PEGGING_ID, A.ORDER_ID, A.ORDER_ID_TYPE, A.START_TIME, A.END_TIME, A.LPST, A.EPST, A.AVAILABLE_TIME, A.FROM_SITE_ID, A.FROM_ROUTE_ID, A.FROM_OPERATION_ID, A.TO_SITE_ID, A.TO_ROUTE_ID, A.TO_OPERATION_ID, A.PLAN_DESCR, A.SUB_RESOURCE_ID, A.LOT_ID from TH_OUT_ORDER_TRACKING A where A.master_id = 'SIMMTECH' and A.PLAN_ID = 'SIMM_20250806_P02' and A.OUT_VERSION_ID = 'SIMM_20250806_P02' and A.ORDER_ID_TYPE ='D' and A.to_route_id = 'S3112_0060' 
			inner join 
			TH_MST_ITEM_ROUTE_SITE B with (nolock)  -- select B.CORPORATION, B.PLANT, B.MASTER_ID, B.PLAN_ID, B.ITEM_ID, B.SITE_ID, B.ROUTE_ID, B.ROUTE_LEVEL, B.OPERATION_ID, B.OPERATION_SEQ, B.FLOW_ID, B.MIN_TAT, B.AVG_TAT, B.MAX_TAT, B.TAT_UOM, B.RESOURCE_SELECTOR_ID, B.YIELD, B.MIN_LOT_SIZE, B.INCREMENT_LOT_SIZE, B.MAX_LOT_SIZE, B.IN_MOVING_LOT_SIZE, B.OUT_MOVING_LOT_SIZE, B.IS_MOVING_LOT_LOCKED, B.IN_MOVE_TIME, B.OUT_MOVE_TIME, B.MOVE_TIME_UOM, B.IS_MOVE_TIME_LOCKED, B.EARLY_PRODUCING_TERM, B.EARLY_PRODUCING_TERM_UOM, B.IS_EPT_LOCKED, B.OPERATION_TYPE, B.IS_VALID, B.BUFFER_TIME, B.BUFFER_TIME_UOM, B.DESCR, B.IRS_ATTB_1, B.IRS_ATTB_2, B.IRS_ATTB_3, B.IRS_ATTB_4, B.IRS_ATTB_5, B.IRS_ATTB_6, B.IRS_ATTB_7, B.IRS_ATTB_8, B.IRS_ATTB_9, B.IRS_ATTB_10, B.IRS_ATTB_11, B.IRS_ATTB_12, B.IRS_ATTB_13, B.IRS_ATTB_14, B.IRS_ATTB_15, B.INSERT_ID, B.INSERT_DTTM, B.UPDATE_ID, B.UPDATE_DTTM from TH_MST_ITEM_ROUTE_SITE B where B.CORPORATION = 'STK' and B.PLANT = 'STK' and B.master_id = 'SIMMTECH' and B.PLAN_ID = 'SIMM_20250806_P04' and IRS_ATTB_10 is not null  ; -- and A.OUT_VERSION_ID = 'SIMM_20250806_P02' and A.ORDER_ID_TYPE ='D' and to_route_id = 'S3112_0060' 
			on B.CORPORATION = 'STK' 
			and B.PLANT = 'STK' 
			and A.master_id = B.master_id 
			and A.plan_id = B.plan_id
			and a.to_site_id = b.site_id	-- TO  같은 것  - site는 집계는 자사 외주 나누지 않음
			and a.to_route_id = b.route_id	-- TO  같은 것 
			--and a.to_operation_id = b.operation_id	-- TO  같은 것 
			and SUBSTRING(A.OUT_ITEM_ID, 1, CHARINDEX('__', A.OUT_ITEM_ID) - 1)  = SUBSTRING(B.item_id , 1, CHARINDEX('__', B.item_id ) - 1) 
			inner join 
			WIP_SIMUL_LIST C with (nolock) -- select RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, DEPT_CODE from  WIP_SIMUL_LIST ;
			on B.IRS_ATTB_5 = C.DEPT_CODE
			and B.IRS_ATTB_8 = C.RESOURCE_CAPA_GROUP_ID
			--and B.IRS_ATTB_15 = C.APS_WIP_ROUTE_GRP_ID  --####  다음 버전부터 나옴 나중에 풀 것. 
	where A.master_id = 'SIMMTECH' and A.PLAN_ID = 'SIMM_20250806_P04' and A.OUT_VERSION_ID = 'SIMM_20250806_P04' and A.ORDER_ID_TYPE ='D'
	--and b.irs_attb_15 = 'APS_WRG_0007'   -- WIP Route Group  --##### 임시 막음. 원래 들어와야 함. MST에 넣어주기로 함. 
	--and b.irs_attb_8 = 'APS_RCG_0064' 
	--and b.IRS_ATTB_5 = 'S3112' 
	--and b.IRS_ATTB_2 = 'MCP23724A00'
	and C.RESOURCE_LEVEL = 1 -- 1레벨만 우선. 2레벨은 추가 설정. 
	--order by C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE
) 
--select * from INCOMING_RST_DETAIL A
--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.DEPT_CODE -- , A.LAYER_INOUT, A.Base_date
--;
, 
INCOMING_RST_LEVEL1_SUM as (
	select 'INCOMING' as GBN, A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, 
		   A.BASE_DATE, sum(operation_qty) as TOT_QTY  -- A.DEPT_CODE, 
	from INCOMING_RST_DETAIL  A with (nolock)
	group by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
	--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
)
--select * from INCOMING_RST_LEVEL1_SUM A order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
,
OUTGOING_RST_DETAIL as (
	select	--  A.*, b.item_id  -- , B.*, C.* 
			C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE,
			--B.IRS_ATTB_5 as DEPT_CODE, 
			B.IRS_ATTB_12 as LAYER_INOUT, 
			B.IRS_ATTB_11 as WIP_GBN, 
			B.ROUTE_ID, B.ROUTE_LEVEL, -- 참고용 
			A.OPERATION_QTY, 
			--A.OUT_ITEM_ID, -- BOD Item  
			--A.ROUTE_ID as OT_ROUTE_ID, -- 참고용 임시 
			A.IN_ITEM_ID, -- BOD Item    --## 다름 
			b.item_id, -- 참조용  
			-- A.END_TIME,
			CONVERT(DATE, DATEADD(MINUTE, -450, A.START_TIME)) AS BASE_DATE -- BASE_START_DATE
			-- b.* , A.*		
			---- A.END_TIME -- BOD 입고 완료시간 
			--CONVERT(DATE, DATEADD(MINUTE, -450, A.END_TIME)) AS DELIVERY_IN_DATE
	from	TH_OUT_ORDER_TRACKING A with (nolock)  -- select top 10 A.MASTER_ID, A.PLAN_ID, A.OUT_VERSION_ID, A.DEMAND_ID, A.TRACK_SEQ, A.OPERATION_SEQ, A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, A.RESOURCE_ID, A.OUT_ITEM_ID, A.COMPLETE_QTY, A.OUT_ITEM_PEGGING_ID, A.IN_ITEM_ID, A.OPERATION_QTY, A.IN_ITEM_PEGGING_ID, A.ORDER_ID, A.ORDER_ID_TYPE, A.START_TIME, A.END_TIME, A.LPST, A.EPST, A.AVAILABLE_TIME, A.FROM_SITE_ID, A.FROM_ROUTE_ID, A.FROM_OPERATION_ID, A.TO_SITE_ID, A.TO_ROUTE_ID, A.TO_OPERATION_ID, A.PLAN_DESCR, A.SUB_RESOURCE_ID, A.LOT_ID from TH_OUT_ORDER_TRACKING A where A.master_id = 'SIMMTECH' and A.PLAN_ID = 'SIMM_20250806_P02' and A.OUT_VERSION_ID = 'SIMM_20250806_P02' and A.ORDER_ID_TYPE ='D' and A.to_route_id = 'S3112_0060' 
			inner join 
			TH_MST_ITEM_ROUTE_SITE B with (nolock)  -- select B.CORPORATION, B.PLANT, B.MASTER_ID, B.PLAN_ID, B.ITEM_ID, B.SITE_ID, B.ROUTE_ID, B.ROUTE_LEVEL, B.OPERATION_ID, B.OPERATION_SEQ, B.FLOW_ID, B.MIN_TAT, B.AVG_TAT, B.MAX_TAT, B.TAT_UOM, B.RESOURCE_SELECTOR_ID, B.YIELD, B.MIN_LOT_SIZE, B.INCREMENT_LOT_SIZE, B.MAX_LOT_SIZE, B.IN_MOVING_LOT_SIZE, B.OUT_MOVING_LOT_SIZE, B.IS_MOVING_LOT_LOCKED, B.IN_MOVE_TIME, B.OUT_MOVE_TIME, B.MOVE_TIME_UOM, B.IS_MOVE_TIME_LOCKED, B.EARLY_PRODUCING_TERM, B.EARLY_PRODUCING_TERM_UOM, B.IS_EPT_LOCKED, B.OPERATION_TYPE, B.IS_VALID, B.BUFFER_TIME, B.BUFFER_TIME_UOM, B.DESCR, B.IRS_ATTB_1, B.IRS_ATTB_2, B.IRS_ATTB_3, B.IRS_ATTB_4, B.IRS_ATTB_5, B.IRS_ATTB_6, B.IRS_ATTB_7, B.IRS_ATTB_8, B.IRS_ATTB_9, B.IRS_ATTB_10, B.IRS_ATTB_11, B.IRS_ATTB_12, B.IRS_ATTB_13, B.IRS_ATTB_14, B.IRS_ATTB_15, B.INSERT_ID, B.INSERT_DTTM, B.UPDATE_ID, B.UPDATE_DTTM from TH_MST_ITEM_ROUTE_SITE B where B.CORPORATION = 'STK' and B.PLANT = 'STK' and B.master_id = 'SIMMTECH' and B.PLAN_ID = 'SIMM_20250806_P04' and IRS_ATTB_10 is not null  ; -- and A.OUT_VERSION_ID = 'SIMM_20250806_P02' and A.ORDER_ID_TYPE ='D' and to_route_id = 'S3112_0060' 
			on B.CORPORATION = 'STK' 
			and B.PLANT = 'STK' 
			and A.master_id = B.master_id 
			and A.plan_id = B.plan_id
			and a.from_site_id = b.site_id	-- TO  같은 것  - site는 집계는 자사 외주 나누지 않음
			and a.from_route_id = b.route_id	-- TO  같은 것 
			--and a.to_operation_id = b.operation_id	-- TO  같은 것 
			--and SUBSTRING(A.IN_ITEM_ID, 1, CHARINDEX('__', A.IN_ITEM_ID) - 1)  = SUBSTRING(B.item_id , 1, CHARINDEX('__', B.item_id ) - 1) 
			and A.IN_ITEM_ID = B.ITEM_ID --## 다름. OUTGOING은 바로 걸어도 됨 
			inner join 
			WIP_SIMUL_LIST C -- select RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, DEPT_CODE from  WIP_SIMUL_LIST ;
			on B.IRS_ATTB_5 = C.DEPT_CODE
			and B.IRS_ATTB_8 = C.RESOURCE_CAPA_GROUP_ID
			--and B.IRS_ATTB_15 = C.APS_WIP_ROUTE_GRP_ID  --####  다음 버전부터 나옴 나중에 풀 것. 안 풀어도 되긴 함. 
	where A.master_id = 'SIMMTECH' and A.PLAN_ID = 'SIMM_20250806_P04' and A.OUT_VERSION_ID = 'SIMM_20250806_P04' and A.ORDER_ID_TYPE ='D'
	--and b.irs_attb_15 = 'APS_WRG_0007'   -- WIP Route Group  --##### 임시 막음. 원래 들어와야 함. MST에 넣어주기로 함. 
	--and b.irs_attb_8 = 'APS_RCG_0064' 
	--and b.IRS_ATTB_5 = 'S3112' 
	--and b.IRS_ATTB_2 = 'MCP23724A00'
	and C.RESOURCE_LEVEL = 1 -- 1레벨만 우선. 2레벨은 추가 설정. 
	--order by C.RESOURCE_LEVEL, C.DIVISION_ID, C.APS_WIP_ROUTE_GRP_ID, C.WIP_ROUTE_GROUP_NAME, C.RESOURCE_CAPA_GROUP_ID, C.RESOURCE_CAPA_GROUP_NAME, C.DEPT_CODE
) 
--select * from OUTGOING_RST_DETAIL A
--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.DEPT_CODE -- , A.LAYER_INOUT, A.Base_date
--;
,
OUTGOING_RST_LEVEL1_SUM as (
	select 'OUTGOING' as GBN, A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, 
		   A.Base_date, sum(operation_qty) as TOT_QTY  -- A.DEPT_CODE, 
	from OUTGOING_RST_DETAIL A with (nolock)
	group by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
	--order by A.RESOURCE_LEVEL, A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date  -- A.DEPT_CODE, 
)
SELECT A.*,BOHTB.BOH as  FIRST_BOH INTO #TEMP_TABLE
FROM (
	select * from INCOMING_RST_LEVEL1_SUM  with (nolock) 
	union all 
	select * from OUTGOING_RST_LEVEL1_SUM  with (nolock) 
) A 
LEFT OUTER JOIN 
(		
			SELECT
				A.DIVISION_ID,
				B.APS_WIP_ROUTE_GRP_ID,
				B.WIP_ROUTE_GROUP_NAME,
				CASE 
					WHEN A.INNER_OUTER = 'I' THEN 'INNER'
					WHEN A.INNER_OUTER = 'O' THEN 'OUTER'
					ELSE ''
				END AS LAYEER_INOUT,
				SUM(A.SQM) AS BOH
			FROM th_tar_wip A WITH (NOLOCK)
			INNER JOIN TH_TAR_DEPT_MASTER_WITH_NAME_V B WITH (NOLOCK)
				ON A.DIVISION_ID = B.DIVISION_ID
				AND A.DEPT_CODE = B.DEPT_CODE
			WHERE A.DIVISION_ID = 'SPS' -- UI 조회조건 입력
			GROUP BY 
				A.DIVISION_ID, 
				B.WIP_ROUTE_GROUP_NAME,
				B.APS_WIP_ROUTE_GRP_ID,
				A.INNER_OUTER
		) AS BOHTB ON A.APS_WIP_ROUTE_GRP_ID = BOHTB.APS_WIP_ROUTE_GRP_ID AND A.LAYER_INOUT = BOHTB.LAYEER_INOUT
where A.RESOURCE_CAPA_GROUP_ID in('APS_RCG_0012','APS_RCG_0031')

;


select *
from 
(
SELECT 		
	 DIVISION_ID,
	 APS_WIP_ROUTE_GRP_ID,
	 WIP_ROUTE_GROUP_NAME,
	 RESOURCE_CAPA_GROUP_ID,
	 RESOURCE_CAPA_GROUP_NAME,
	 LAYER_INOUT,
	'NEW+WIP' WIP_GBN,	
	'EOH1' GBN,
	RESOURCE_LEVEL,
	1 V_LEVEL,
	BASE_DATE,	
	0 FIRST_BOH,
	0 TOT_QTY	
	FROM #TEMP_TABLE A  with (nolock) 
	WHERE BASE_DATE BETWEEN   '2025-08-06' and '2025-08-07' 
	group by RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date 
	UNION ALL 
	select 	
	DIVISION_ID,
	APS_WIP_ROUTE_GRP_ID,
	WIP_ROUTE_GROUP_NAME,
	RESOURCE_CAPA_GROUP_ID,
	RESOURCE_CAPA_GROUP_NAME,
	LAYER_INOUT,
	WIP_GBN,	
	GBN,
	RESOURCE_LEVEL,
	2 V_LEVEL,
	BASE_DATE,	
	0 FIRST_BOH,
	TOT_QTY	
	FROM #TEMP_TABLE A with (nolock) 
	WHERE BASE_DATE BETWEEN   '2025-08-06' and '2025-08-07' 
	UNION ALL 
	SELECT 
	 DIVISION_ID,
	 APS_WIP_ROUTE_GRP_ID,
	 WIP_ROUTE_GROUP_NAME,
	 RESOURCE_CAPA_GROUP_ID,
	 RESOURCE_CAPA_GROUP_NAME,
	 LAYER_INOUT,
	 WIP_GBN,	
	'EOH3' GBN,
	RESOURCE_LEVEL,
	3 V_LEVEL,
	BASE_DATE,	
	0 FIRST_BOH,
	0 TOT_QTY
	FROM #TEMP_TABLE A with (nolock) 
	WHERE BASE_DATE BETWEEN '2025-08-06' and '2025-08-07' 
	group by RESOURCE_LEVEL, DIVISION_ID, APS_WIP_ROUTE_GRP_ID, WIP_ROUTE_GROUP_NAME, RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, A.LAYER_INOUT, A.WIP_GBN, A.Base_date 

) AS SourceTable
PIVOT (
   SUM(TOT_QTY)
   FOR BASE_DATE IN (
				[2025-08-06], [2025-08-07]
					)
) AS PivotResult
order by  RESOURCE_LEVEL,DIVISION_ID, WIP_ROUTE_GROUP_NAME,   RESOURCE_CAPA_GROUP_NAME ,LAYER_INOUT, WIP_GBN,V_LEVEL,GBN
;




*/