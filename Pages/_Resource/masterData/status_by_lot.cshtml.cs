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
    public class status_by_lot : BasePageModel
    {
        public status_by_lot()
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
                //vali.Null("item_code", "Item Code�� �Է����ּ���.");

                vali.DoneDeco();

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("item_code", "�۾��з��ڵ尡 �Էµ��� �ʾҽ��ϴ�.");
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
			// ����1
			var Task1 = Task.Run(() =>
			{

				StringBuilder sSQL = new StringBuilder();

				sSQL.Append($@"
with 
plan_info as (
	--select '20250519' as plan_start_date 
	select master_id, plan_id, plan_start_dttm, DATEADD(DAY, 1, PLAN_START_DTTM) as PLAN_DATE_END_DTTM  -- 20250630
	from th_eng_plan_info with (nolock)
	where master_id = 'SIMMTECH' and plan_id = {terms["PLAN_ID"].V} 
), 
cufoff_Date as (
select plan_id, PLAN_ATTB_1 as WIP_YYYYMMDD,  PLAN_ATTB_2 as WIP_SEQ
from  th_mst_plan with (nolock)
WHERE PLAN_ID = {terms["PLAN_ID"].V} 
), 
cutoff_wip as (
	select	ITEM_CODE, REVISION, JOB_ID, JOB_NAME, INNER_OUTER,
			WORKING_TYPE, WORK_STATUS, 		
			--ORGANIZATION_ID, 
			DEPT_CODE, OPERATION_SEQ_NUM, RESOURCE_CODE, RESOURCE_NAME, SCH_DATE, 
			--FIRST_UNIT_START_DATE, COMP_DATE, COMP_DATE2, 
			-- DELTA, WAIT_TIME, 
            WAIT_TIME,
			SQM, PRODUCT_WPNL, PRODUCT_PCS, 
			PRODUCT_M2,  -- SUBCONTRACTOR_FLAG, REPEAT, INNER_OUTER, MFG_CATEGORY, PATTERN, INPUT_YIELD, NEXT_OPERATION_SEQ_NUM 
			SUBCONTRACTOR_FLAG
	from TH_TAR_WIP_HIS with (nolock)
	where 1=1
    and yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
	and SEQ = (select WIP_SEQ from cufoff_Date)
	and USE_YN = 'Y' 
	--and item_code = 'BOC04420C00'
	--order by ITEM_CODE, REVISION, OPERATION_SEQ_NUM desc, working_type, JOB_NAME,JOB_ID, sch_date
),
DEMAND AS (
	select  A.demand_id  
	from	th_mst_demand A with (nolock)
			inner join 
			plan_info B
			on A.master_id = B.master_id
			AND A.PLAN_ID = B.PLAN_ID 
), 
WIP_DEMAND_YN as (
	select	A.*, 
			-- B.demand_id as MST_DEMAND_ID, 
			case when isnull(B.demand_id, 'No DEMAND' ) = 'No DEMAND' then 'N'  else 'Y' end as DEMAND_YN 
	from	cutoff_wip A
			left outer join 
			DEMAND B
			on A.job_name = B.DEMAND_ID
),
-- ���� �۾� ��� (����. �̵�) 
work_list as (
	select	-- A.*, 
			A.DEMAND_ID
    from	TH_OUT_ORDER_TRACKING A  with (nolock) 
			inner join
			plan_info B 
			on A.master_id = B.master_id
			and A.plan_id = B.plan_id 
			and A.out_version_id = B.plan_id 
            inner join 
			TH_DPP_NET_DEMAND C with (nolock)
			ON  A.master_id = C.master_id
			AND A.plan_id = C.plan_id
			AND A.demand_id = C.demand_id
			AND C.corporation = 'STK'
			AND C.plant = 'STK'
			--inner join 
			--th_mst_item_route_site C 
			--on A.master_id = C.master_id
			--and A.plan_id = C.plan_id 
			--and A.out_version_id = C.plan_id 
			--and A.SITE_ID = C.SITE_ID
			--and A.ROUTE_ID = C.ROUTE_ID
			--and A.OUT_ITEM_ID = C.ITEM_ID
	where 1=1
	--and A.master_id = 'SIMMTECH' 
	--and A.plan_id = 'SIMM_20250826_P01' 
	--and A.out_version_id = 'SIMM_20250826_P01'
	and A.start_time between B.PLAN_START_DTTM and PLAN_DATE_END_DTTM  -- 1��ġ�� 
	and A.site_id not in ('V_I','V_O') 
	--order by start_time 
	group by A.DEMAND_ID
),
complete_operation as (
	select  -- MASTER_ID, PLAN_ID, OUT_VERSION_ID, WORK_ORDER_ID, 
			substring(A.ITEM_ID, charindex('_',  A.ITEM_ID)+1, case when charindex('__',  A.ITEM_ID) > 0 then charindex('__',  A.ITEM_ID) - charindex('_',  A.ITEM_ID) -1 else len(A.ITEM_ID) -  charindex('_',  A.ITEM_ID) end ) as job_name,  
			--A.ITEM_ID, 
			--A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, 		
			C.IRS_ATTB_2 AS ITEM_CODE, C.IRS_ATTB_3 AS REVISION, C.IRS_ATTB_4 AS ST_SITE, C.IRS_ATTB_5 AS DEPT_CODE, C.IRS_ATTB_6 AS DEPT_NAME, ROUTE_LEVEL AS OPER_SEQ, -- SEQ �´���? 
			--C.IRS_ATTB_8 as RESOURCE_CAPA_GROUP_ID, 
			--D.RESOURCE_CAPA_GROUP_NAME, 
			A.RESOURCE_ID, --B.RESOURCE_CAPA_GROUP_NAME, 
			B.RESOURCE_NAME, 		
			C.AVG_TAT AS PROCESSING_TIME_HR, C.BUFFER_TIME AS ESSENTIAL_WAITING_TIME_HR, C.IN_MOVE_TIME AS WAITING_TIME_HR, 
			--A.START_TIME, A.END_TIME, -- ENG ORGINAL
			-- UI�� �۾�/���ð� 
			dateadd(SS, - C.BUFFER_TIME*60.0*60.0 - C.IN_MOVE_TIME*60.0*60.0, A.START_TIME ) AS CLOCK_ACCEPT_TIME,  -- ���ð� �������� ���. WIP 
			A.START_TIME AS PROCESS_START_TIME, 
			dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS PROCESS_END_TIME,
			dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS CLOCK_OUT_TIME,
			--A.OPERATION_TIME, A.TOTAL_WORKING_TIME, 
			A.OPERATION_QTY --, COMPLETE_QTY, BATCH_QTY
			--,DEPT_CLASS_CODE, DEPT_CLASS_NAME, 
			--JOB_NAME, -- ������ ���� ����. MST_ITEM_ROUTE_SITE�� JOB_NAME attribute �߰��� ��.  
	from	TH_OUT_WORK_ORDER A with (nolock)
			INNER JOIN 
			plan_info AA 
			on A.MASTER_ID = AA.MASTER_ID
			and A.plan_id =  AA.Plan_id 
			and a.OUT_VERSION_ID = AA.Plan_id 
			left outer join
			TH_MST_RESOURCE B with (nolock)
			on A.MASTER_ID = B.MASTER_ID
			and A.plan_id =  B.Plan_id 
			and A.RESOURCE_ID = B.RESOURCE_ID
			left outer join 
			th_mst_item_route_site C with (nolock)
			on A.MASTER_ID = C.MASTER_ID
            and C.PLANT = 'STK'
			and C.CORPORATION = 'STK'
			and A.plan_id =  C.Plan_id 
			and A.SITE_ID = C.SITE_id
			and A.ITEM_ID = C.item_id
			and A.route_id = C.route_id 			
	where	1=1
	--and		A.master_id = 'SIMMTECH' 
	--and		A.plan_id = 'SIM_20250630_004' 
	--and		A.out_version_id = 'SIM_20250630_004' 
	--and		A.start_time between '2025-07-01 07:30:00' and '2025-07-02 07:30:00' 
	--and		A.start_time between '2025-06-30 07:30:00' and '2025-07-01 07:30:00' 
	--and		A.start_time between AA.PLAN_START_DTTM and AA.PLAN_START_DTTM +1
	and		A.resource_id in ( 'V_O') 
	--and A.item_id like '%AS24D0046-01%' 
	--order by job_name, oper_seq, A.start_time, A.resource_id, A.item_id ;
	--order by  A.resource_id, A.start_time, job_name, oper_seq, A.item_id ;
),
rst1 as (
	select	A.*, 
			--B.RESOURCE_CAPA_GROUP_ID, B.RESOURCE_CAPA_GROUP_NAME, 
			--B.CLOCK_OUT_TIME  -- �԰����� -- job_name, ITEM_CODE, CLOCK_OUT_TIME 
			convert(varchar(8), B.CLOCK_OUT_TIME, 112) as COMPLETE_DATE -- �԰�����
			--datediff(A.SCH_DATE, B.COMPLETE_DATE)
			-- A.WORKING_TYPE --> Hold ���� 
	from	WIP_DEMAND_YN A
			left outer join 
			complete_operation B
			--on A.JOB_ID = B.JOB_ID 
			on A.JOB_NAME = B.JOB_NAME 
	-- order by A.demand_yn desc , A.item_code, A.revision, A.job_name
), 
rst2 as (
	select	B.DIVISION_ID, B.APS_WIP_ROUTE_GRP_ID, B.WIP_ROUTE_GROUP_NAME, B.SITE_ID, 
			A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, A.WORKING_TYPE, A.WORK_STATUS, A.INNER_OUTER,
			--B.RESOURCE_CAPA_GROUP_ID, 		
			A.OPERATION_SEQ_NUM, 
			B.RESOURCE_CAPA_GROUP_NAME, -- ���� ���� �׷� 
			A.DEPT_CODE, B.DEPT_NAME, -- ���� ���� ���� 
			A.RESOURCE_CODE, A.RESOURCE_NAME, 
			A.SCH_DATE, A.COMPLETE_DATE, 
            A.WAIT_TIME,
			--convert(date, SCH_DATE) as sch_date1,   
			--convert(date, COMPLETE_DATE) as COMPLETE_DATE1,  
			datediff(DD, convert(date, A.SCH_DATE), convert(date, A.COMPLETE_DATE)  ) as expected_delay_date,
			A.SQM, A.PRODUCT_M2, A.DEMAND_YN, A.SUBCONTRACTOR_FLAG,
            A.PRODUCT_WPNL, A.PRODUCT_PCS
	from	rst1 A
			left outer join 
			TH_TAR_DEPT_MASTER_WITH_NAME_V B with (nolock)
			on A.dept_code = B.DEPT_CODE 
			--left outer join 
			--RESOURCE_CAPA_GROUP_V D
			--on A.IRS_ATTB_8 = D.RESOURCE_CAPA_GROUP_ID
	--order by A.demand_yn desc, A.item_code, A.revision, A.job_name, A.COMPLETE_DATE
) 
,
urgent_level_list as (
	select	SEGMENT1 as URGENT_LEVEL,			-- DB �Է� ������ 
			ATTRIBUTE02 as URGENT_LEVEL_NAME,	-- UI ǥ�� ������ 
			sort_order  -- ��� ���Ŀ�
	from LOOKUP_VALUE_M with (nolock) where LOOKUP_TYPE_CODE = 'LT_REDUCTION_RATE_BY_URGENCY_LEVEL' 
	and LOOKUP_TYPE_VERSION = ( select LOOKUP_TYPE_VERSION 
								from LOOKUP_TYPE_M 
								where LOOKUP_TYPE_CODE = 'LT_REDUCTION_RATE_BY_URGENCY_LEVEL' 
								and active_flag = 'Y' ) 
	and ACTIVE_FLAG = 'Y' 
	--order by sort_order  
)
select	A.DIVISION_ID, A.WIP_ROUTE_GROUP_NAME, A.SITE_ID,
        CASE WHEN G.DEMAND_ID IS NULL THEN 'N' ELSE 'Y' END AS PLAN_YN,
        E.URGENT_LEVEL_NAME AS URJENT,
        A.WAIT_TIME,
        C.Finish AS ""ǥ��ó��"",
        F.THICKNESS AS ""THK"",
        C.THICK AS FINAL_THK,
        F.HOLE_TYPE_NAME AS HOLE,
        F.PATTERN_CU_TYPE AS FILL_TYPE,
        CASE 
		    WHEN A.INNER_OUTER = 'O' THEN 'OUTER'
		    WHEN A.INNER_OUTER = 'I' THEN 'INNER'
		    ELSE null
		END AS INNER_OUTER,
        B.PATTERN_GUBUN, B.CUSTOMER_NAME AS CUSTOMER, B.MODEL_NAME AS MODEL, B.D_LAYER AS LAYER, B.LAYUP_TYPE AS LAYUP,
		B.CATEGORY3 AS MFG_CATEGORY, C.CCL_THICK AS CCL, C.BBT_TYPE, --C.OSP, C.SOP,
		A.ITEM_CODE, A.REVISION, A.JOB_ID, A.JOB_NAME, 
		A.WORKING_TYPE, A.WORK_STATUS, -- Hold ���� ǥ�� 
		A.OPERATION_SEQ_NUM, 
		A.RESOURCE_CAPA_GROUP_NAME, -- ���� ���� �׷� 
		A.DEPT_CODE, A.DEPT_NAME, -- ���� ���� ���� 
		A.RESOURCE_CODE, A.RESOURCE_NAME, 
		A.SCH_DATE, A.COMPLETE_DATE, 
		case when A.EXPECTED_DELAY_DATE <= 0 then 'Y' when A.EXPECTED_DELAY_DATE > 0 then 'N' else '' end as OTD_YN, --  �����ؼ� ���� 
		A.EXPECTED_DELAY_DATE,		-- ���� ���� ���� 
		A.SQM, A.PRODUCT_M2, A.PRODUCT_WPNL, A.PRODUCT_PCS,
		A.DEMAND_YN, -- Demand ���� ���� (���� ���� : WIP Routing ���� ��) 
		A.SUBCONTRACTOR_FLAG -- ���� ���� 
from 
    rst2 A
    left join
    TH_GUI_ITEM_BY_PROCESS_GUBUN B with (nolock)
    ON A.ITEM_CODE = B.ITEM_CODE
    left join
    TH_GUI_ITEM_MODEL_SEARCH C with (nolock)
    ON B.ITEM_CODE = C.ITEM_CODE
    left join
    TH_TAR_URGENT_LOT D with (nolock)
    ON A.JOB_NAME = D.JOB_NAME
    left join
    urgent_level_list E 
    ON D.URGENCY_LEVEL = E.URGENT_LEVEL
    LEFT JOIN (
	    SELECT ITEM_CODE, REVISION, DEPARTMENT_CODE, OPERATION_SEQ, THICKNESS, HOLE_TYPE_NAME, PATTERN_CU_TYPE 
	    FROM (
	        SELECT *
	        FROM TH_TAR_ROUTING_L with (nolock)
	    ) sub
	) F
	ON A.ITEM_CODE = F.ITEM_CODE
	AND A.REVISION = F.REVISION
	AND A.DEPT_CODE = F.DEPARTMENT_CODE
    AND A.OPERATION_SEQ_NUM = F.OPERATION_SEQ
    left join work_list G
    ON A.JOB_NAME = G.DEMAND_ID
    LEFT JOIN (
    	select * from WIP_ROUTE_GROUP_V3
    ) H
    ON A.WIP_ROUTE_GROUP_NAME = H.WIP_ROUTE_GROUP_NAME
    AND A.DIVISION_ID = H.DIVISION_ID
where
	1=1
");
                if (terms["group_id"].Length > 0)
                {
                    sSQL.Append($@"
	and A.DIVISION_ID = {terms["group_id"].V}
");
                }

                if (terms["item_code"].Length > 0)
				{
					sSQL.Append($@"
	and A.ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
				}

                if (terms["customer"].Length > 0)
                {
                    sSQL.Append($@"
    and B.CUSTOMER_NAME LIKE '%{terms["customer"].AsString()}%'
");
                }

                if (terms["wip_route"].Length > 0)
                {
                    sSQL.Append($@"
	and A.APS_WIP_ROUTE_GRP_ID = {terms["wip_route"].V}
");
                }

                if (terms["lot"].Length > 0)
                {
                    sSQL.Append($@"
    and A.JOB_NAME LIKE '%{terms["lot"].AsString()}%'
");
                }

                sSQL.Append(@"
order by A.DIVISION_ID ASC, H.SORT_ORDER ASC, A.item_code, A.OPERATION_SEQ_NUM, A.WORKING_TYPE
-- ���� �������� ( 2025-10-21 OJT / �������ϻ��� 127�� �ݿ� )
--order by A.demand_yn desc, A.item_code, A.revision, A.job_name, A.COMPLETE_DATE
");

                Console.WriteLine(sSQL.ToString());

                return Data.Get(sSQL.ToString()).Tables[0];

            });

            // ����1
            var Task2 = Task.Run(() =>
            {
                var sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT  A.MASTER_ID
      , A.PLAN_ID
      , A.OUT_VERSION_ID
      , A.DEMAND_ID         AS JOB_NAME
      , A.ROUTING_CNT       AS '�ܿ� ����'
      , A.TAT               AS TAT
      , B.DEPT_NAME          AS 'MAX ��Ⱑ ū �� �� MIN ����'
      , B.BUFFER_TIME       AS 'MAX ����ϼ�'
      , B.TOTAL_BUFFER_TIME AS '�Ѵ���ϼ�'
FROM  (
      select  master_id
            , PLAN_ID
            , OUT_VERSION_ID
            , DEMAND_ID
            , COUNT(*) AS ROUTING_CNT
            , DATEDIFF(DAY, MIN(START_TIME), MAX(END_TIME))  AS TAT
      from th_out_order_tracking WITH(NOLOCK)
      where master_id       = 'SIMMTECH'
      and   PLAN_ID         = {terms["PLAN_ID"].V}
      AND   OUT_VERSION_ID  = {terms["PLAN_ID"].V}
      AND   ORDER_ID_TYPE   = 'W'
");
                if (terms["lot"].Length > 0)
                {
                    sSQL.Append($@"
    and DEMAND_ID LIKE '%{terms["lot"].AsString()}%'
");
                }



            sSQL.Append($@"
      GROUP BY  master_id
              , PLAN_ID
              , OUT_VERSION_ID
              , DEMAND_ID
      ) A
      INNER JOIN
      (
      SELECT
              A.MASTER_ID
            , A.PLAN_ID
            , A.OUT_VERSION_ID
            , A.DEMAND_ID
            , A.ROUTE_ID AS ROUTE_ID
            , A.DEPARTMENT_NAME AS DEPT_NAME
            , B.BUFFER_TIME
            , B.TOTAL_BUFFER_TIME
      FROM  (
            select  A.MASTER_ID
                  , A.PLAN_ID
                  , A.OUT_VERSION_ID
                  , A.DEMAND_ID
                  , LEFT(B.ROUTE_ID, CHARINDEX('_', B.ROUTE_ID) - 1) AS ROUTE_ID
                  , C.DEPARTMENT_NAME
                  , ROUND(DATEDIFF(MINUTE, A.END_TIME, B.START_TIME) / 1440.0, 1) AS BUFFER_TIME
            from  TH_OUT_ORDER_TRACKING A WITH(NOLOCK)
                  INNER JOIN
                  TH_OUT_ORDER_TRACKING B WITH(NOLOCK)
                            ON    A.MASTER_ID       = B.MASTER_ID
                            AND   A.PLAN_ID         = B.PLAN_ID
                            AND   A.OUT_VERSION_ID  = B.OUT_VERSION_ID
                            AND   A.DEMAND_ID       = B.DEMAND_ID
                            AND   A.OUT_ITEM_ID     = B.IN_ITEM_ID
                            AND   B.ORDER_ID_TYPE   = 'W'
                            AND   B.SITE_ID         NOT IN ('V_I', 'V_O') -- �ű� �߰� ����
                          LEFT OUTER JOIN
                          TH_TAR_DEPT_MASTER C WITH(NOLOCK)
                          ON LEFT(B.ROUTE_ID, CHARINDEX('_', B.ROUTE_ID) - 1) = DEPARTMENT_CODE
            where A.master_id       = 'SIMMTECH'
            and   A.PLAN_ID         = {terms["PLAN_ID"].V}
            AND   A.OUT_VERSION_ID  = {terms["PLAN_ID"].V}
            AND   A.ORDER_ID_TYPE   = 'W'
            AND   A.SITE_ID         NOT IN ('V_I', 'V_O')  -- �ű� �߰� ����
");
                if (terms["lot"].Length > 0)
                {
                    sSQL.Append($@"
    and A.DEMAND_ID LIKE '%{terms["lot"].AsString()}%'
");
                }


            sSQL.Append($@"
            ) A
            INNER JOIN
            (
            select  A.MASTER_ID
                  , A.PLAN_ID
                  , A.OUT_VERSION_ID
                  , A.DEMAND_ID
                  , ROUND(MAX(DATEDIFF(MINUTE, A.END_TIME, B.START_TIME) / 1440.0), 1) AS BUFFER_TIME
                  , ROUND(SUM(DATEDIFF(MINUTE, A.END_TIME, B.START_TIME) / 1440.0), 1) AS TOTAL_BUFFER_TIME
            from  th_out_order_tracking A WITH(NOLOCK)
                  INNER JOIN
                  TH_OUT_ORDER_TRACKING B WITH(NOLOCK)
            ON    A.MASTER_ID       = B.MASTER_ID
            AND   A.PLAN_ID         = B.PLAN_ID
            AND   A.OUT_VERSION_ID  = B.OUT_VERSION_ID
            AND   A.DEMAND_ID       = B.DEMAND_ID
            AND   A.OUT_ITEM_ID     = B.IN_ITEM_ID
            AND   B.ORDER_ID_TYPE   = 'W'
            where A.master_id       = 'SIMMTECH'
            and   A.PLAN_ID         = {terms["PLAN_ID"].V}
            AND   A.OUT_VERSION_ID  = {terms["PLAN_ID"].V}
            AND   A.ORDER_ID_TYPE   = 'W'                  
            AND   A.SITE_ID         NOT IN ('V_I', 'V_O')  -- �ű� �߰� ����
");
                if (terms["lot"].Length > 0)
                {
                    sSQL.Append($@"
    and A.DEMAND_ID LIKE '%{terms["lot"].AsString()}%'
");
                }


                sSQL.Append($@"
            GROUP BY  A.MASTER_ID
                    , A.PLAN_ID
                    , A.OUT_VERSION_ID
                    , A.DEMAND_ID
            ) B
      ON    A.MASTER_ID       = B.MASTER_ID
      AND   A.PLAN_ID         = B.PLAN_ID
      AND   A.OUT_VERSION_ID  = B.OUT_VERSION_ID
      AND   A.DEMAND_ID       = B.DEMAND_ID
      AND   A.BUFFER_TIME     = B.BUFFER_TIME  -- �ű� �߰� ����
      ) B
ON    A.MASTER_ID       = B.MASTER_ID
AND   A.PLAN_ID         = B.PLAN_ID
AND   A.OUT_VERSION_ID  = B.OUT_VERSION_ID
AND   A.DEMAND_ID       = B.DEMAND_ID
WHERE  A.PLAN_ID = {terms["PLAN_ID"].V}
ORDER BY B.BUFFER_TIME DESC--A.ROUTING_CNT
");

                //Console.WriteLine(sSQL.ToString());

                return Data.Get(sSQL.ToString()).Tables[0];
            });

            // ����3 --> Routing ���� ��ȸ
            var Task3 = Task.Run(() =>
            {
                StringBuilder sSQL = new StringBuilder();

                sSQL.Append($@"
-- SM �μ�
WITH SM_PRINT AS (
    SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'F30 Vacuum Lami(Flat)' THEN 1
            WHEN DEPARTMENT_NAME = 'F30 Vacuum Lami(DFSR)' THEN 2
            WHEN DEPARTMENT_NAME = 'F52 Vacuum Lami(Flat)' THEN 3
            WHEN DEPARTMENT_NAME = 'F52 Vacuum Lami(DFSR)' THEN 4
            WHEN DEPARTMENT_NAME = 'FS1 Vacuum Lami(DFSR)' THEN 5
            WHEN DEPARTMENT_NAME = 'FS1 Vacuum Lami(Flat)' THEN 6
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- �뱤 TYPE
EXPOSURE_TYPE AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'FS1 SM Exposure(DI_2D)' THEN 1
            WHEN DEPARTMENT_NAME = 'F52 SM Exposure(DI_2D)' THEN 2
            WHEN DEPARTMENT_NAME = 'F30 SM Exposure(DI_2D)' THEN 3
            WHEN DEPARTMENT_NAME = 'FS1 SM Exposure(DI)' THEN 4
            WHEN DEPARTMENT_NAME = 'F52 SM Exposure(DI)' THEN 5
            WHEN DEPARTMENT_NAME = 'F30 SM Exposure(DI)' THEN 6
            WHEN DEPARTMENT_NAME = 'FS1 SM Exposure' THEN 7
            WHEN DEPARTMENT_NAME = 'F52 SM Exposure' THEN 8
            WHEN DEPARTMENT_NAME = 'F30 SM Exposure' THEN 9
            WHEN DEPARTMENT_NAME = 'F30 SM Exposure(HDI)' THEN 10
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- SM ����
SM_CUR AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'FS1 SM V Develop' THEN 1
            WHEN DEPARTMENT_NAME = 'F30 SM V Develop' THEN 2
            WHEN DEPARTMENT_NAME = 'F52 SM Develop' THEN 3
            WHEN DEPARTMENT_NAME = 'F30 SM Dev_Cure' THEN 4
            WHEN DEPARTMENT_NAME = 'F30 SM Dev_Cure(HDI)' THEN 5
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- PLASMA
PLASMA AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'F52 SM Plasma(CF4)2nd' THEN 1
            WHEN DEPARTMENT_NAME = 'F30 SM Plasma(CF4)2nd' THEN 2
            WHEN DEPARTMENT_NAME = 'F52 SM Plasma(CF4)' THEN 3
            WHEN DEPARTMENT_NAME = 'F30 SM Plasma(CF4)' THEN 4
            WHEN DEPARTMENT_NAME = 'F52 SM Plasma SEC(CF4)' THEN 5
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- MASK
MASK AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'F52 Exposure(Au/M2)LDI' THEN 1
            WHEN DEPARTMENT_NAME = 'FS3 Exposure_P(Au/M2)' THEN 2
            WHEN DEPARTMENT_NAME = 'F52 Exposure_P(Au/M2)' THEN 3
            WHEN DEPARTMENT_NAME = 'FS3 Exposure(Au/M2)' THEN 4
            WHEN DEPARTMENT_NAME = 'F52 Exposure(Au/M2)' THEN 5
            WHEN DEPARTMENT_NAME = 'F52 Exposure(Au D/M2)' THEN 6
            WHEN DEPARTMENT_NAME = 'FS3 Exposure(Au D/M2)' THEN 7
            WHEN DEPARTMENT_NAME = 'F52 Exposure(Au/M1)LDI' THEN 8
            WHEN DEPARTMENT_NAME = 'F52 Exposure_P(Au/M1)' THEN 9
            WHEN DEPARTMENT_NAME = 'F52 Exposure(Au/M1)' THEN 10
            WHEN DEPARTMENT_NAME = 'F52 Exposure(Au D/M1)' THEN 11
            WHEN DEPARTMENT_NAME = 'FS3 Exposure_P(Au/M1)' THEN 12
            WHEN DEPARTMENT_NAME = 'FS3 Exposure(Au D/M1)' THEN 13
            WHEN DEPARTMENT_NAME = 'FS3 Exposure(Au/M1)' THEN 14
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- OSP
OSP AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'FS2 OSP(TAMURA)' THEN 1
            WHEN DEPARTMENT_NAME = 'FS2 OSP(SHIKOKU)' THEN 2
            WHEN DEPARTMENT_NAME = 'F53 OSP' THEN 3
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- SOP
SOP AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'FS2 SOP Coining' THEN 1
            WHEN DEPARTMENT_NAME = 'F11 SOP Coining' THEN 2
            WHEN DEPARTMENT_NAME = 'F53 SOP Coining' THEN 3
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- SINGLATION
SINGLATION AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'F11 Singulation' THEN 1
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- LAZ
LAZ AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'F31 DES(Laz)' THEN 1
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- EB
EB AS (
	SELECT 
        ITEM_CODE,
        REVISION,
        DEPARTMENT_NAME, 
        CASE 
            WHEN DEPARTMENT_NAME = 'DES (O/L)_E/B' THEN 1
            ELSE 999
        END AS PRIORITY
    FROM TH_TAR_ROUTING_L
),
-- Hot Press ���� Ƚ�� ���
HOT_PRESS_COUNT AS (
    SELECT 
        ITEM_CODE,
        REVISION,
        COUNT(*) AS HOT_PRESS_COUNT
    FROM TH_TAR_ROUTING_L
    WHERE DEPARTMENT_NAME IN (
        'Hot Press',
        'F30 Hot Press',
        'F30 Hot Press(SPS)',
        'F91 Hot Press'
    )
    GROUP BY ITEM_CODE, REVISION
)
SELECT 
    COALESCE(A.ITEM_CODE, B.ITEM_CODE, C.ITEM_CODE, D.ITEM_CODE, E.ITEM_CODE, F.ITEM_CODE, G.ITEM_CODE, H.ITEM_CODE, I.ITEM_CODE, J.ITEM_CODE, K.ITEM_CODE) AS ITEM_CODE,
    COALESCE(A.REVISION, B.REVISION, C.REVISION, D.REVISION, E.REVISION, F.REVISION, G.REVISION, H.REVISION, I.REVISION, J.REVISION, K.REVISION) AS REVISION,
    A.DEPARTMENT_NAME AS SM_PRINT,
    B.DEPARTMENT_NAME AS NO_GUANG_TYPE,
    C.DEPARTMENT_NAME AS SM_CUR,
    D.DEPARTMENT_NAME AS PLASMA,
    E.DEPARTMENT_NAME AS MASK,
    F.DEPARTMENT_NAME AS OSP,
    G.DEPARTMENT_NAME AS SOP,
    H.DEPARTMENT_NAME AS SINGLATION,
    I.DEPARTMENT_NAME AS LAZ,
    J.DEPARTMENT_NAME AS EB,
    K.HOT_PRESS_COUNT
FROM (
    SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM SM_PRINT
    WHERE PRIORITY < 999) A 
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM EXPOSURE_TYPE
    WHERE PRIORITY < 999
) B ON A.ITEM_CODE = B.ITEM_CODE AND A.REVISION = B.REVISION
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM SM_CUR
    WHERE PRIORITY < 999
) C ON A.ITEM_CODE = C.ITEM_CODE AND A.REVISION = C.REVISION	
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM PLASMA
    WHERE PRIORITY < 999
) D ON A.ITEM_CODE = D.ITEM_CODE AND A.REVISION = D.REVISION	
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM MASK
    WHERE PRIORITY < 999
) E ON A.ITEM_CODE = E.ITEM_CODE AND A.REVISION = E.REVISION	
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM OSP
    WHERE PRIORITY < 999
) F ON A.ITEM_CODE = F.ITEM_CODE AND A.REVISION = F.REVISION
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM SOP
    WHERE PRIORITY < 999
) G ON A.ITEM_CODE = G.ITEM_CODE AND A.REVISION = G.REVISION
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM SINGLATION
    WHERE PRIORITY < 999
) H ON A.ITEM_CODE = H.ITEM_CODE AND A.REVISION = H.REVISION
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM LAZ
    WHERE PRIORITY < 999
) I ON A.ITEM_CODE = I.ITEM_CODE AND A.REVISION = I.REVISION
full outer join (
	SELECT 
        ITEM_CODE, 
        REVISION,
        DEPARTMENT_NAME
    FROM EB
    WHERE PRIORITY < 999
) J ON A.ITEM_CODE = J.ITEM_CODE AND A.REVISION = J.REVISION
left join HOT_PRESS_COUNT K ON A.ITEM_CODE = K.ITEM_CODE AND A.REVISION = K.REVISION
Order by ITEM_CODE
");

                //Console.WriteLine(sSQL.ToString());

                return Data.Get(sSQL.ToString()).Tables[0];
            });

            // ����4
            var Task4 = Task.Run(() =>
            {
                StringBuilder sSQL = new StringBuilder();

                sSQL.Append($@"
select 
	A.ITEM_CODE
	, A.REVISION
	, B.DESCRIPTION	AS SM_COLOR
from 
	cbst_spec_capa2 A
	left join Cbst_common_l B
	on A.ORGANIZATION_ID = 101
	AND A.SMASK_COLOR = B.line_code
where
	B.lookup_type = 'S/M Color'
	AND B.enabled_flag = 'Y'
");

                return Data.Get(sSQL.ToString()).Tables[0];
            });


            // ����Ʈ�� �־ � ���ǿ� ���� �ش��ϴ� Task�� ���ư����� ���� �ʿ�
            var taskList = new List<Task<DataTable>>();

            taskList.Add(Task1);
            taskList.Add(Task2);
            taskList.Add(Task3);
            taskList.Add(Task4);


            // ��� ������ ���� �Ϸ� �Ǳ� ��ٸ� 
            //Task.WaitAll(Task1, Task2, Task3, Task4, Task5, Task6);
            Task.WaitAll(taskList.ToArray());

            // Ÿ�� �ƿ� ����� 
            //if(Task.WaitAll(new Task[] { Task1, Task2, Task3 }, 10000) == false) // 10���� Ÿ�� �ƿ�
            //    throw new Exception("������ �ð��� �ʰ� �Ͽ����ϴ�.(10��)"); 

            DataTable[] resultTables = new[]
            {
                Task1.Result
                ,Task2.Result
                ,Task3.Result
                ,Task4.Result
            };

            // 1. ���� 1���� 2���� JOB_NAME �������� ����
            DataTable dt12 = resultTables[0].Copy();
            MergeDataTableByKeys(dt12, resultTables[1], new[] { "JOB_NAME" });

            // 2. �� ����, dt12�� 3���� ITEM_CODE, REVISION �������� ����
            DataTable dtMain = dt12.Copy();
            MergeDataTableByKeys2(dtMain, resultTables[2], new[] { "ITEM_CODE", "REVISION" });
            MergeDataTableByKeys2(dtMain, resultTables[3], new[] { "ITEM_CODE", "REVISION" });

            //return dt12;
            return dtMain;

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

        // baseTable�� extraTable �����͸� merge
        public static void MergeDataTableByKeys(DataTable baseTable, DataTable extraTable, string[] keyColumns)
        {
            
            // 1. �÷� ĳ�� �� �߰�
            var baseColumnNames = new HashSet<string>(baseTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            foreach (DataColumn col in extraTable.Columns)
            {
                if (!baseColumnNames.Contains(col.ColumnName))
                {
                    baseTable.Columns.Add(col.ColumnName, col.DataType);
                    baseColumnNames.Add(col.ColumnName);
                }
            }

            // 2. Ű ���� �Լ� (null/���� ���� ó��)
            string GetKey(DataRow row) =>
                string.Join("|", keyColumns.Select(k =>
                {
                    var val = row[k];
                    if (val == null || val == DBNull.Value) return "";

                    string strVal = val.ToString().Trim();

                    // REVISION �÷��̸� 3�ڸ��� �е�
                    if (k.Equals("Rev", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(strVal, out int revNum))
                        {
                            return revNum.ToString("D3"); // "1" �� "001"
                        }
                    }

                    return strVal;
                }));

            // 3. baseTable �ε���
            var baseIndex = baseTable.AsEnumerable()
                .ToDictionary(row => GetKey(row), row => row);

            // 4. ����
            foreach (DataRow extraRow in extraTable.Rows)
            {
                string key = GetKey(extraRow);
                if (baseIndex.TryGetValue(key, out DataRow baseRow))
                {
                    foreach (DataColumn col in extraTable.Columns)
                    {
                        string colName = col.ColumnName;
                        if (keyColumns.Contains(colName)) continue;

                        baseRow[colName] = extraRow[colName];
                    }
                }
            }
        }

        // �ߺ������Ѱɷ�
        public static void MergeDataTableByKeys2(DataTable baseTable, DataTable extraTable, string[] keyColumns)
        {
            // 1. baseTable�� ���� �÷� �߰�
            var baseColumnNames = new HashSet<string>(baseTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            foreach (DataColumn col in extraTable.Columns)
            {
                if (!baseColumnNames.Contains(col.ColumnName))
                {
                    baseTable.Columns.Add(col.ColumnName, col.DataType);
                    baseColumnNames.Add(col.ColumnName);
                }
            }

            // 2. Ű ���� �Լ�
            string GetKey(DataRow row) =>
                string.Join("|", keyColumns.Select(k =>
                {
                    var val = row[k];
                    if (val == null || val == DBNull.Value) return "";

                    string strVal = val.ToString().Trim();

                    if (k.Equals("Rev", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(strVal, out int revNum))
                            return revNum.ToString("D3");
                    }
                    return strVal;
                }));

            // 3. baseTable �ε��� (�ߺ� Ű ���)
            var baseIndex = new Dictionary<string, List<DataRow>>();
            foreach (DataRow row in baseTable.Rows)
            {
                string key = GetKey(row);

                if (!baseIndex.ContainsKey(key))
                    baseIndex[key] = new List<DataRow>();

                baseIndex[key].Add(row);
            }

            // 4. ����
            foreach (DataRow extraRow in extraTable.Rows)
            {
                string key = GetKey(extraRow);


                if (baseIndex.TryGetValue(key, out List<DataRow> baseRows))
                {
                    foreach (DataRow baseRow in baseRows)
                    {
                        foreach (DataColumn col in extraTable.Columns)
                        {
                            string colName = col.ColumnName;
                            if (keyColumns.Contains(colName)) continue;

                            baseRow[colName] = extraRow[colName];
                        }
                    }
                }

            }
        }


    }
}
