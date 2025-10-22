using DocumentFormat.OpenXml.Drawing;
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
    public class work_order : BasePageModel
    {
        public work_order()
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

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];

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


            // 쿼리1
            var Task1 = Task.Run(() =>
            {
                StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
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
cutoff_wip as (
	select	JOB_NAME, INNER_OUTER, DEPT_CODE, OPERATION_SEQ_NUM, PRODUCT_WPNL, PRODUCT_PCS, PRODUCT_M2, WAIT_TIME, SCH_DATE
	from TH_TAR_WIP_HIS with (nolock)
	where 1=1
    and yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
	and SEQ = (select WIP_SEQ from cufoff_Date)
	and USE_YN = 'Y' 
	--and item_code = 'BOC04420C00'
	--order by ITEM_CODE, REVISION, OPERATION_SEQ_NUM desc, working_type, JOB_NAME,JOB_ID, sch_date
),
PST as (
	select master_id, plan_id, plan_start_dttm, DATEADD(DAY, 1, PLAN_START_DTTM) as PLAN_DATE_END_DTTM  --1일치 계획만, 시업시간 고려. 
	from th_eng_plan_info with (nolock) 
	where master_id = 'SIMMTECH' and plan_id = {terms["PLAN_ID"].V} -- UI 조회 Plan ID 
),
complete_operation as (
        select  -- MASTER_ID, PLAN_ID, OUT_VERSION_ID, WORK_ORDER_ID,
                        substring(A.ITEM_ID, charindex('_',  A.ITEM_ID)+1, case when charindex('__',  A.ITEM_ID) > 0 then charindex('__',  A.ITEM_ID) - charindex('_',  A.ITEM_ID) -1 else len(A.ITEM_ID) -  charindex('_',  A.ITEM_ID) end ) as job_name,
                        --A.ITEM_ID,
                        --A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID,
                        C.IRS_ATTB_2 AS ITEM_CODE, C.IRS_ATTB_3 AS REVISION, C.IRS_ATTB_4 AS ST_SITE, C.IRS_ATTB_5 AS DEPT_CODE, C.IRS_ATTB_6 AS DEPT_NAME, ROUTE_LEVEL AS OPER_SEQ, -- SEQ 맞는지?
                        --C.IRS_ATTB_8 as RESOURCE_CAPA_GROUP_ID,
                        --D.RESOURCE_CAPA_GROUP_NAME,
                        A.RESOURCE_ID, --B.RESOURCE_CAPA_GROUP_NAME,
                        B.RESOURCE_NAME,
                        C.AVG_TAT AS PROCESSING_TIME_HR, C.BUFFER_TIME AS ESSENTIAL_WAITING_TIME_HR, C.IN_MOVE_TIME AS WAITING_TIME_HR,
                        --A.START_TIME, A.END_TIME, -- ENG ORGINAL
                        -- UI용 작업/대기시간
                        dateadd(SS, - C.BUFFER_TIME*60.0*60.0 - C.IN_MOVE_TIME*60.0*60.0, A.START_TIME ) AS CLOCK_ACCEPT_TIME,  -- 대기시간 역산으로 계산. WIP
                        A.START_TIME AS PROCESS_START_TIME,
                        dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS PROCESS_END_TIME,
                        dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS CLOCK_OUT_TIME,
                        --A.OPERATION_TIME, A.TOTAL_WORKING_TIME,
                        A.OPERATION_QTY --, COMPLETE_QTY, BATCH_QTY
                        --,DEPT_CLASS_CODE, DEPT_CLASS_NAME,
                        --JOB_NAME, -- 가져올 곳이 없음. MST_ITEM_ROUTE_SITE에 JOB_NAME attribute 추가할 것.
        from    TH_OUT_WORK_ORDER A with (nolock)
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
        where   1=1
        and             A.resource_id in ( 'V_O')
),
-- 당일 작업 목록 (공정. 이동) 
work_list as (
	select	-- A.*, 
			A.master_id, A.plan_id, A.out_Version_id, A.demand_id as job_name, A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, A.START_TIME, A.END_TIME, A.RESOURCE_ID, A.OUT_ITEM_ID, A.OPERATION_QTY, A.ORDER_ID_TYPE, 
			lag(end_time) over (partition by A.DEMAND_ID order by start_time, end_Time) as incoming_dttm, -- 앞 이동 또는 공정 종료 시간 = 현 공정에 Lot 들어온 시간 
			B.plan_start_dttm, C.DMD_ATTB_15 AS DELTA
                  , convert(varchar(8), D.CLOCK_OUT_TIME, 112) as COMPLETE_DATE -- 입고예상일
    from	TH_OUT_ORDER_TRACKING A  with (nolock) 
			inner join
			PST B 
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
            left outer join complete_operation D
            on A.demand_id = D.JOB_NAME
	where 1=1
	and A.site_id not in ('V_I','V_O') 
");
                if (terms["start_date"].Length > 0)
                {
                    sSQL.Append($@"
    and A.start_time >= '{terms["start_date"].AsString() + " 07:30:00"}'
");
                }

                if (terms["end_date"].Length > 0)
                {
                    var endDate = DateTime.Parse(terms["end_date"].AsString()).AddDays(1);
                    sSQL.Append($@"
    and A.start_time < '{endDate.ToString("yyyy-MM-dd")} 07:30:00'
");
                }

                sSQL.Append($@"
) 
,
urgent_level_list as (
	select	SEGMENT1 as URGENT_LEVEL,			-- DB 입력 데이터 
			ATTRIBUTE02 as URGENT_LEVEL_NAME,	-- UI 표시 데이터 
			sort_order  -- 목록 정렬용
	from LOOKUP_VALUE_M where LOOKUP_TYPE_CODE = 'LT_REDUCTION_RATE_BY_URGENCY_LEVEL' 
	and LOOKUP_TYPE_VERSION = ( select LOOKUP_TYPE_VERSION 
								from LOOKUP_TYPE_M 
								where LOOKUP_TYPE_CODE = 'LT_REDUCTION_RATE_BY_URGENCY_LEVEL' 
								and active_flag = 'Y' ) 
	and ACTIVE_FLAG = 'Y' 
	--order by sort_order  
)
-- select count(*) from  work_list where ORDER_ID_TYPE = 'W' ;
select  --A.*, -- C.*  
        A.COMPLETE_DATE,
        H.SCH_DATE, H.PRODUCT_WPNL, H.PRODUCT_PCS, H.PRODUCT_M2,H.WAIT_TIME, E.THICK AS FINAL_THK, E.FINISH, K.THICKNESS, K.HOLE_TYPE_NAME, K.PATTERN_CU_TYPE, N.PATTERN_GUBUN,N.CATEGORY3,
        H.OPERATION_SEQ_NUM AS CUR_OPER_SEQ, H.DEPT_CODE AS CUR_DEPT_CODE, I.DEPARTMENT_NAME AS CUR_DEPT_NAME, C.IRS_ATTB_12 AS INNER_OUTER,
        A.DELTA, E.CUSTOMER, E.MODEL_NAME, E.LAYER, E.LAYUP_TYPE, E.MFG_CATEGORY, E.CCL_THICK, E.THICK,
        M.URGENT_LEVEL_NAME AS URJENT,
		D.DIVISION_ID,
		D.DEPARTMENT_CLASS_CODE AS DEPT_CLASS_CODE,
		D.DEPARTMENT_CLASS_NAME AS DEPT_CLASS_NAME,
		E.CUSTOMER,		
		A.JOB_NAME, --A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, A.RESOURCE_ID, -- A.OUT_ITEM_ID, 
		A.OPERATION_QTY, --A.ORDER_ID_TYPE,
		C.IRS_ATTB_2 AS ITEM_CODE, C.IRS_ATTB_3 AS REVISION, --C.IRS_ATTB_4 AS ST_SITE, -- SITE가 외주로 안 나옴
		C.IRS_ATTB_5 AS DEPT_CODE, C.IRS_ATTB_6 AS DEPT_NAME, ROUTE_LEVEL AS OPER_SEQ, -- SEQ 맞는지? 
		A.RESOURCE_ID, B.RESOURCE_GROUP_ID, J.RESOURCE_CAPA_GROUP_NAME,
		B.RESOURCE_NAME,
        B.RES_ATTB_3 AS SITE_ID, 
		--## 2025.08.27. 수정 
		C.IRS_ATTB_13 AS PROCESSING_TIME_HR,  -- 가공시간. Plan Option의 가공 LEAD TIME(%) 비율 반영값임.
		C.IRS_ATTB_14 AS ESSENTIAL_WAITING_TIME_HR, -- 필수대기 시간 
		C.IN_MOVE_TIME AS WAITING_TIME_HR, -- 대기시간. Plan Option의 대기 LEAD TIME(%) 비율 반영값임.
		--A.START_TIME, A.END_TIME, -- ENG ORGINAL
		-- UI용 작업/대기시간 
		-- A.incoming_dttm, -- 이전 BOD 또는 공정 End time 
		--dateadd(SS, - C.BUFFER_TIME*60.0*60.0 - C.IN_MOVE_TIME*60.0*60.0, A.START_TIME ) AS CLOCK_ACCEPT_TIME_ORG,  -- 대기시간 역산으로 계산. (미사용) 
		--isnull(A.incoming_dttm, dateadd(SS, - C.BUFFER_TIME*60.0*60.0 - C.IN_MOVE_TIME*60.0*60.0, A.START_TIME ) )  AS CLOCK_ACCEPT_TIME, -- incoming_dttm = 이전 BOD 또는 공정 End time. 없으면  대기시간 역산으로 계산. --> PST로 변경 	
		isnull(A.incoming_dttm, A.plan_start_dttm )  AS CLOCK_ACCEPT_TIME, -- 공정에 들어온 시간. incoming_dttm = 이전 BOD 또는 공정 End time. 없으면 Plan Start Time으로 설정(첫 공정이므로).
		A.START_TIME AS PROCESS_START_TIME, -- 제거. 일자로 변경. start time 기준 SEQ 표시. 지금 UI에는 주야간 표시를 시간으로 판정한 듯.
        A.END_TIME AS PROCESS_END_TIME,
		CONVERT(DATE, DATEADD(MINUTE, -450, A.START_TIME)) AS START_DATE,  -- 시업시간 반영한 작업일.  
		rank() over (partition by A.RESOURCE_ID order by A.START_TIME)  as WORK_SEQ, -- 순번 채번 기준 확인 
        CASE 
            WHEN 
        	    CAST(FORMAT(dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME), 'HH:mm') AS TIME) 
	                    BETWEEN '07:30' AND '20:30' THEN 'DAY'
	            ELSE 'NIGHT'
	        END AS SHIFT,
		--dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS PROCESS_END_TIME,
		--dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME ) AS CLOCK_OUT_TIME,
		--dateadd(SS, (IRS_ATTB_13+C.IRS_ATTB_14)*60.0*60.0, A.START_TIME ) AS CLOCK_OUT_TIME, 
		A.END_TIME as CLOCK_OUT_TIME, -- 공정 완료 후 나가는 시간 
		--A.OPERATION_TIME, A.TOTAL_WORKING_TIME, 
		A.OPERATION_QTY --, COMPLETE_QTY, BATCH_QTY
        , F.REMARK --미달사유
        , F.SMESSAGE --전달사항
from	work_list A 
		left outer join
		TH_MST_RESOURCE B with (nolock) 
		on A.MASTER_ID = B.MASTER_ID
		and A.plan_id =  B.Plan_id 
		and A.RESOURCE_ID = B.RESOURCE_ID
        AND B.corporation = 'STK'
		AND B.plant = 'STK'
		inner join 
		th_mst_item_route_site C with (nolock) 
		on A.MASTER_ID = C.MASTER_ID
		and A.plan_id =  C.Plan_id 
		and A.SITE_ID = C.SITE_id
		and A.OUT_ITEM_ID = C.item_id
		and A.route_id = C.route_id 	
        AND C.corporation = 'STK'
		AND C.plant = 'STK'
		left outer join
		TH_TAR_DEPT_MASTER D with (nolock)
		on C.IRS_ATTB_5 = D.DEPARTMENT_CODE
		left outer join
		TH_GUI_ITEM_MODEL_SEARCH E with (nolock)
		ON C.IRS_ATTB_2 = E.ITEM_CODE
        left outer join
        TH_GUI_WORK_ORDER_REMARK F with (nolock)
        ON F.PLAN_ID = A.PLAN_ID
        AND A.JOB_NAME = F.JOB_NAME
        AND D.DEPARTMENT_CLASS_CODE = F.DEPT_CLASS_CODE
        AND C.IRS_ATTB_5 = DEPT_CODE
        AND C.IRS_ATTB_2 = F.ITEM_CODE
        ANd C.IRS_ATTB_3 = F.REVISION
        left outer join
        cutoff_wip H
        ON A.JOB_NAME = H.JOB_NAME
        left outer join
        TH_TAR_DEPT_MASTER I with (nolock)
        ON H.DEPT_CODE = I.DEPARTMENT_CODE
        left outer join
        APS_SITE_RESOURCE_MES_CAPA_V J with (nolock)
        ON J.APS_RESOURCE_ID = B.RESOURCE_ID
        LEFT JOIN (
		    SELECT ITEM_CODE, REVISION, DEPARTMENT_CODE, OPERATION_SEQ, THICKNESS, HOLE_TYPE_NAME, PATTERN_CU_TYPE 
		    FROM (
		        SELECT *
		        FROM TH_TAR_ROUTING_L with (nolock)
		    ) sub
		) K
		ON C.IRS_ATTB_2 = K.ITEM_CODE
		AND C.IRS_ATTB_3 = K.REVISION
		AND C.IRS_ATTB_5 = K.DEPARTMENT_CODE
		AND ROUTE_LEVEL = K.OPERATION_SEQ
        left join
        TH_TAR_URGENT_LOT L
        ON A.JOB_NAME = L.JOB_NAME
        left join
        urgent_level_list M
        ON L.URGENCY_LEVEL = M.URGENT_LEVEL
       left join
		(
		SELECT (ITEM_CODE),MAX(CATEGORY3) AS CATEGORY3,MAX(PATTERN_GUBUN)  PATTERN_GUBUN   FROM TH_GUI_ITEM_BY_PROCESS_GUBUN 
		GROUP BY ITEM_CODE
		) N 
		ON C.IRS_ATTB_2 = N.ITEM_CODE
where	A.master_id = 'SIMMTECH' 
--and		A.plan_id = 'SIMM_20250826_P01' 
--and		A.out_version_id = 'SIMM_20250826_P01' 
and		A.ORDER_ID_TYPE = 'W' 
and		A.resource_id not in ('V_I', 'V_O') 
and		C.IRS_ATTB_11 != 'NEW'  -- 신규 투입은 작업지시 생성 제외 요청 (8/26)
");

                if (terms["item_code"].Length > 0)
                {
                    sSQL.Append($@"
	and C.IRS_ATTB_2 LIKE '%{terms["item_code"].AsString()}%'
");
                }

                if (terms["group_id"].Length > 0)
                {
                    sSQL.Append($@"
	and D.DIVISION_ID = {terms["group_id"].V}
");
                }

                if (terms["dept_class_name"].Length > 0)
                {
                    sSQL.Append($@"
	and D.DEPARTMENT_CLASS_CODE = {terms["dept_class_name"].V}
");
                }

                if (terms["dept_name"].Length > 0)
                {
                    sSQL.Append($@"
	and C.IRS_ATTB_5 = {terms["dept_name"].V}
");
                }

                if (terms["customer"].Length > 0)
                {
                    sSQL.Append($@"
	and E.CUSTOMER LIKE '%{terms["customer"].AsString()}%'
");
                }

                if (terms["shift"].Length > 0)
                {
                    sSQL.Append($@"
	and CASE 
        WHEN 
        	CAST(FORMAT(dateadd(SS, C.AVG_TAT*60.0*60.0, A.START_TIME), 'HH:mm') AS TIME) 
	             BETWEEN '07:30' AND '20:30' THEN 'DAY'
	        ELSE 'NIGHT'
	    END = {terms["shift"].V}
");
                }

                sSQL.Append(@"
-- 정렬 순서변경 ( 2025-10-21 OJT / 수정보완사항 126번 반영 )
order by  D.DEPARTMENT_CLASS_NAME, C.IRS_ATTB_6, B.RES_ATTB_3, C.IRS_ATTB_2, H.OPERATION_SEQ_NUM DESC
--order by  A.resource_id, A.start_time, --job_name, 
--		  oper_seq, C.item_id ;
");
                Console.WriteLine(sSQL.ToString());

                return Data.Get(sSQL.ToString()).Tables[0];
            });


            // 쿼리2
            var Task2 = Task.Run(() =>
            {
                StringBuilder sSQL = new StringBuilder();

                sSQL.Append($@"
WITH 
-- SM 인쇄
SM_PRINT AS (
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
    FROM TH_TAR_ROUTING_L with (nolock)
),
-- 노광 TYPE
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
    FROM TH_TAR_ROUTING_L with (nolock)
),
-- SM 현상
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
    FROM TH_TAR_ROUTING_L with (nolock)
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
    FROM TH_TAR_ROUTING_L with (nolock)
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
    FROM TH_TAR_ROUTING_L with (nolock)
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
    FROM TH_TAR_ROUTING_L with (nolock)
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
    FROM TH_TAR_ROUTING_L with (nolock)
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
    FROM TH_TAR_ROUTING_L with (nolock)
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
    FROM TH_TAR_ROUTING_L with (nolock)
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
    FROM TH_TAR_ROUTING_L with (nolock)
),
-- Hot Press 적층 횟수 계산
HOT_PRESS_COUNT AS (
    SELECT 
        ITEM_CODE,
        REVISION,
        COUNT(*) AS HOT_PRESS_COUNT
    FROM TH_TAR_ROUTING_L with (nolock)
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

                Console.WriteLine(sSQL.ToString());

                return Data.Get(sSQL.ToString()).Tables[0];
            });

            // 쿼리3
            var Task3 = Task.Run(() =>
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




            // 리스트에 넣어서 어떤 조건에 들어가면 해당하는 Task만 돌아가도록 구현 필요
            var taskList = new List<Task<DataTable>>();

            taskList.Add(Task1);
            taskList.Add(Task2);
            taskList.Add(Task3);


            // 모든 쿼리가 실행 완료 되길 기다림 
            //Task.WaitAll(Task1, Task2, Task3, Task4, Task5, Task6);
            Task.WaitAll(taskList.ToArray());

            // 타임 아웃 적용시 
            //if(Task.WaitAll(new Task[] { Task1, Task2, Task3 }, 10000) == false) // 10초의 타임 아웃
            //    throw new Exception("지정된 시간이 초과 하였습니다.(10초)"); 

            DataTable[] resultTables = new[]
            {
                Task1.Result
                ,Task2.Result
                ,Task3.Result
        };

            // 4. 병합
            DataTable dtMain = resultTables[0].Copy();

            string[] keyColumns = new[] { "ITEM_CODE", "REVISION" };

            for (int i = 1; i < resultTables.Length; i++)
            {
                MergeDataTableByKeys(dtMain, resultTables[i], keyColumns);
            }

            return dtMain;
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
        private void Save(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"  

DECLARE @PLAN_ID nvarchar(50);
DECLARE @DEPT_CLASS_CODE nvarchar(100);
DECLARE @DEPT_CODE nvarchar(100);
DECLARE @JOB_NAME nvarchar(100);
DECLARE @ITEM_CODE nvarchar(100);
DECLARE @REVISION nvarchar(100);
DECLARE @REMARK nvarchar(500);
DECLARE @SMESSAGE nvarchar(500);



            ");
            foreach (Params ITEM in dataList)
            {
                DTClient.UserInfoMerge(ITEM);
                sSQL.Append($@"  
SET @PLAN_ID  = {ITEM["PLAN_ID"].V};
SET @DEPT_CLASS_CODE  = {ITEM["DEPT_CLASS_CODE"].V};
SET @DEPT_CODE  = {ITEM["DEPT_CODE"].V};
SET @JOB_NAME  = {ITEM["JOB_NAME"].V};
SET @ITEM_CODE = {ITEM["ITEM_CODE"].V};
SET @REVISION  = {ITEM["REVISION"].V};
SET @REMARK = {ITEM["REMARK"].V};
SET @SMESSAGE = {ITEM["SMESSAGE"].V};


IF EXISTS (
    SELECT 1 FROM TH_GUI_WORK_ORDER_REMARK WHERE PLAN_ID = @PLAN_ID 
    AND DEPT_CLASS_CODE = @DEPT_CLASS_CODE AND DEPT_CODE = @DEPT_CODE
    AND JOB_NAME = @JOB_NAME AND  ITEM_CODE = @ITEM_CODE 
    AND REVISION = @REVISION
)
BEGIN
    -- UPDATE 로직

      UPDATE TH_GUI_WORK_ORDER_REMARK SET           
            REMARK = @REMARK,
            SMESSAGE = @SMESSAGE,
            UPDATE_ID = '{Cookie<User>.Store.USER_ID}',
            UPDATE_DTTM = GETDATE()
       WHERE PLAN_ID = @PLAN_ID 
    AND DEPT_CLASS_CODE = @DEPT_CLASS_CODE AND DEPT_CODE = @DEPT_CODE
    AND JOB_NAME = @JOB_NAME AND  ITEM_CODE = @ITEM_CODE 
    AND REVISION = @REVISION;

END
ELSE
BEGIN
        -- INSERT 로직
        INSERT INTO TH_GUI_WORK_ORDER_REMARK (
            PLAN_ID, 
            DEPT_CLASS_CODE,
            DEPT_CODE,
            JOB_NAME, 
            ITEM_CODE, 
            REVISION,
            REMARK, 
            SMESSAGE,
            INSERT_ID, 
            INSERT_DTTM
        ) VALUES (
            @PLAN_ID,
            @DEPT_CLASS_CODE,
            @DEPT_CODE,
            @JOB_NAME, 
            @ITEM_CODE, 
            @REVISION, 
            @REMARK, 
            @SMESSAGE,
            '{Cookie<User>.Store.USER_ID}',
            GETDATE()
        )

END;

");
            }

            HS.Web.Common.Data.Execute(sSQL.ToString());
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


        public static void MergeDataTableByKeys(DataTable baseTable, DataTable extraTable, string[] keyColumns)
        {
            // 1. baseTable에 없는 컬럼 추가
            var baseColumnNames = new HashSet<string>(baseTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            foreach (DataColumn col in extraTable.Columns)
            {
                if (!baseColumnNames.Contains(col.ColumnName))
                {
                    baseTable.Columns.Add(col.ColumnName, col.DataType);
                    baseColumnNames.Add(col.ColumnName);
                }
            }

            // 2. 키 생성 함수
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

            // 3. baseTable 인덱싱 (중복 키 허용)
            var baseIndex = new Dictionary<string, List<DataRow>>();
            foreach (DataRow row in baseTable.Rows)
            {
                string key = GetKey(row);

                if (!baseIndex.ContainsKey(key))
                    baseIndex[key] = new List<DataRow>();

                baseIndex[key].Add(row);
            }

            // 4. 병합
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
