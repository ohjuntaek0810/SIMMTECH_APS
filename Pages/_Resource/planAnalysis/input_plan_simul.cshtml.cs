using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
//using GrapeCity.DataVisualization.TypeScript;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class input_plan_simul : BasePageModel
    {
        public input_plan_simul()
        {
            this.Handler = handler;
            Params result = HS.Web.Common.ApsManage.searchPlanId().ToParams();
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

            else if (e.Command == "search_version")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchVersion(terms);
            }

            else if (e.Command == "new_version")
            {
                Params terms = e.Params["terms"];
                // 새로운 투입계획 버전 만들기
                this.SaveNewVersion(terms);
            }

            else if (e.Command == "save")
            {
                ParamList terms = e.Params["data"];
				string version = e.Params["CUR_VERSION"];

                // 투입계획 저장
                this.Save(terms, version);
            }

            else if (e.Command == "aps_input_date_update")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.ApsInputDateUpdate(terms);
            }

            else if (e.Command == "update_plan_id")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.updatePlanId(terms);
            }

            if (e.Command == "search_inputstock")
            {
                Params rowterms = e.Params["rowterms"];
                toClient["DetailData"] = this.Searchinputstock(rowterms);
                return toClient;
            }


            return toClient;
        }

		/// <summary>
		/// 조회 로직 
		/// </summary>
		/// <param name="terms"></param>
		/// <returns></returns>
		private DataTable SearchVersion(Params terms)
		{

			DTClient.UserInfoMerge(terms);

			StringBuilder sSQL = new StringBuilder();

			sSQL.Append(@$"
            select * from TH_TAR_INPUT_PLAN_DIVISION_H WHERE DIVISION_ID = {terms["group_id"].V} and PLAN_DATE between {terms["start_date"].V} and {terms["end_date"].V}
            order by VERSION DESC
            ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }




        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable Search(Params terms)
        {

            DTClient.UserInfoMerge(terms);

			StringBuilder checkSQL = new StringBuilder();
			checkSQL.Append($@"
select * from TH_TAR_INPUT_PLAN_L WHERE 1=1
");
			if (terms["VERSION"].Length > 0)
			{
				checkSQL.Append($@"
and VERSION = {terms["VERSION"].V}
");
			}

			DataTable dt = Data.Get(checkSQL.ToString()).Tables[0];


            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
-- 버전이 SPS, HDI 두 가지로 업로드 될 것. 
with 
BOOKED_INFO as (
	select * from openquery(ERP_RUN,   'select  HEADER_ID, ORG_ID, ORDER_TYPE_ID, ORDER_NUMBER, VERSION_NUMBER, ORDERED_DATE, CANCELLED_FLAG, BOOKED_FLAG, BOOKED_DATE, FLOW_STATUS_CODE 
										from    oe_order_headers_all where ORDERED_DATE > = sysdate - 365')  -- 기본 1년치 조회 
	--order by ordered_Date 
),
--SPS 버전, HDI 버전 --## UI에서 선택 또는 최신 버전 
TH_TAR_READY_BY_INPUT_L_V as (
	select	*
	from	TH_TAR_READY_BY_INPUT_L A 
	where	
            1=1
            AND ITEM_CODE IS NOT NULL
");
            if (terms["group_id"].Length > 0)
            {
                if (terms["group_id"].AsString() == "SPS")
                {
                    sSQL.Append($@"
            AND A.PROD_GROUP = 'S' 
");
                }
                else if (terms["group_id"].AsString() == "HDI")
                {
                    sSQL.Append($@"
            AND A.PROD_GROUP = 'H' 
");
                }
            }


            sSQL.Append($@"
) 
,
PPG_EXPIRATION as (
	select 
		ASSEMBLY_ITEM_CODE AS ITEM_CODE
		, MAX(EXPIRATION_DATE) AS PPG_EXPIRTATION_DATE
		, MAX(EXPIRATION_DAY) AS PPG_EXPIRATION_DAY
	from 
		TH_TAR_READY_BY_INPUT_MATERIAL_L
	WHERE -- VERSION = 'SPS_20251021_024'
		CTYPE = 'APG'
");
            if (terms["group_id"].Length > 0)
            {
                if (terms["group_id"].AsString() == "SPS")
                {
                    sSQL.Append($@"
            AND PRODUCT_GROUP_CODE = 'S' 
");
                }
                else if (terms["group_id"].AsString() == "HDI")
                {
                    sSQL.Append($@"
            AND PRODUCT_GROUP_CODE = 'H' 
");
                }
            }

            sSQL.Append($@"
	group by ASSEMBLY_ITEM_CODE
)
,
ITEM_REV_LIST as (
	select	DISTINCT A.ITEM_CODE, (select max(B.revision) from cbst_spec_basic B where B.item_code = A.item_code) as REVISION
	from	TH_TAR_READY_BY_INPUT_L_V A 
	--order by A.ITEM_CODE, REVISION
)
--검증 
--select	A.*, B.ITEM_CODE, B.REVISION
--from	ITEM_REV_LIST A
--		left outer join 
--		--th_tar_routing_h B
--		(
--			select B.ITEM_CODE, B.REVISION from th_tar_routing_L B group by  B.ITEM_CODE, B.REVISION
--		) B
--		on a.ITEM_CODE=b.item_code
--		and a.revision = b.revision
--order by A.ITEM_CODE, A.REVISION
--;
,
APS_LT as (
	select  A.ITEM_CODE, A.REVISION, 
			--B.*, 
			B.OPERATION_SEQ, B.DEPARTMENT_CODE, --B.DEPARTMENT_NAME, 
			C.PLAN_PROCESSING_TIME_HR, C.PLAN_ESSENTIAL_WAITING_TIME_HR, PLAN_OPER_WAITING_TIME_HR, 
			isnull(C.PLAN_PROCESSING_TIME_HR, 0) + isnull(C.PLAN_ESSENTIAL_WAITING_TIME_HR, 0) + isnull(PLAN_OPER_WAITING_TIME_HR, 0) as ITEM_ROUTE_LT_HR
	from	ITEM_REV_LIST A
			left outer join 
			th_tar_routing_L B  -- select top 10 * from th_tar_routing_L where  use_yn = 'Y'
			on  A.item_code = B.item_code 
			and A.revision = B.revision
			and B.DEPARTMENT_ID > 0 
			--and B.USE_YN = 'Y'  --## 2025.08.18. 빼야 함 
			left outer join 
			TH_TAR_PLAN_DEPT_LEADTIME C  --  select top 10 * from TH_TAR_PLAN_DEPT_LEADTIME
			on b.organization_id = C.ORGANIZATION_ID
			and B.DEPARTMENT_CODE = C.DEPARTMENT_CODE
	where 1=1
	--and A.item_Code = 'BOC00023A00'--  'MCP23369A00'  -- 임시 
	--and B.use_yn = 'Y'
)
--select * from APS_LT
,
APS_ITEM_TOT_LT as (
	select item_code, revision, sum(ITEM_ROUTE_LT_HR)/24 as ITEM_APS_LT_DAY
	from APS_LT 
	group by item_code, revision
), 
MATERIAL_EXPIRATION as (
	select	A.INVENTORY_ITEM_ID, A.ITEM_CODE, A.MIN_EXPIRATION_DATE  
	from	TH_TAR_MATERIAL_INVENTORY_L A  -- select * from  TH_TAR_MATERIAL_INVENTORY_L  -- CUTOFF_DATE, SEQ, REVISION, INVENTORY_ITEM_ID, ITEM_CODE, MIN_EXPIRATION_DATE 
			inner join 
			TH_TAR_MATERIAL_INVENTORY_H B -- select * from  TH_TAR_MATERIAL_INVENTORY_H
			on A.CUTOFF_DATE = B.CUTOFF_DATE
			and		A.REVISION = B.REVISION 
	where B.use_yn = 'Y'   -- 현재 유효 버전 적용  --> 투입계획 Header에 이 버전을 입력해줄 것 
)
--,
--BBT_CAPA as (
---- ERP12 BBT JIG 현황 : STC MFG > BBT Jig Inquery  -- 운영 접근 안됨 
--		select * from openquery(ERP_RUN,   '
--				SELECT jig_code, capa_pcs
--				FROM   cbwip_bbt_jig_list_v
--				WHERE  organization_id = 101
--				AND    (category_nm = ''S'')
--		')
--) 
--select * from BBT_CAPA
--select * from APS_ITEM_TOT_LT;

--insert into TH_TAR_INPUT_PLAN_L
--   (VERSION, CUST_NAME, CUSTOMER_NUMBER, SHIP_TO, END_CUST, BOOKED_FLAG, ITEM_CODE, MODEL_NAME, SO_NUMBER, SCHEDULE_LINE_ID, SCHEDULE_LINE_SEQ, 
--		STD_LEAD_TIME, ATP_LEAD_TIME, APS_LEAD_TIME, STD_LT_INPUT_DATE, ATP_LT_INPUT_DATE, APS_LT_INPUT_DATE, NEW_DATE, ORDER_DATE, WAITING_DAYS, 
--		READY_BY_INPUT_PNL_QTY, READY_BY_INPUT_SQM_QTY, READY_BY_INPUT_PCS_QTY, APS_PLAN_INPUT_DATE, INPUT_PNL_QTY, SQM_PER_PNL_RATIO, PCS_PER_PNL_RATIO, NEW_START_DATE, DESCRIPTION, 
--		CCL_EXPIRATION_DATE, CCL_EXPIRATION_DAYS, PPG_EXPIRATION_DATE, PPG_EXPIRATION_DAYS, BBT_YN, BBT_JIG_CAPA, LOT_SIZE, SHRINKAGE_RATE, INSERT_ID, INSERT_DTTM, UPDATE_ID, UPDATE_DTTM
--		)
select	A.VERSION, A.CUST_NAME, 
		(select max(B.customer) from cbst_spec_basic  B where B.ITEM_CODE = A.ITEM_CODE ) as CUSTOMER_NUMBER, -- 쓸 일이 있을지?  
		null SHIP_TO, null END_CUST, B.BOOKED_FLAG, 
		A.ITEM_CODE, A.MODEL_NAME, 
		A.SO_NUMBER, 
		A.SCHEDULE_LINE_ID, 
		1 as SCHEDULE_LINE_SEQ, -- 초기값은 1, 분할하게 되면 행 추가하면서 1 증가
		A.LEAD_TIME as STD_LEAD_TIME, null ATP_LEAD_TIME, 
		D.ITEM_APS_LT_DAY as APS_LEAD_TIME, 
		A.NEW_DATE - A.LEAD_TIME as STD_LT_INPUT_DATE, null ATP_LT_INPUT_DATE, 
		A.NEW_DATE - D.ITEM_APS_LT_DAY as APS_LT_INPUT_DATE, --A.PROD_GROUP, A.CCL_QTY, A.FLM, A.MFG_CATEGORY, A.LAYER, 
		--A.LAYUP_DESC,   A.MODEL_NAME, A.LEAD_TIME, 
		--A.NEW_START_DATE, 
		A.NEW_DATE, -- 납기 
		-- A.ORDER_DATE, -- UI 미표시 
		datediff(DAY, A.ORDER_DATE , getdate() ) as WAITING_DAYS,
		A.PNL_QTY as READY_BY_INPUT_PNL_QTY, A.M2_QTY as READY_BY_INPUT_SQM_QTY, A.QTY_PCS as READY_BY_INPUT_PCS_QTY, 
		null as APS_PLAN_INPUT_DATE,				-- APS 투입계획일자. Plan ID 가지고 별도 계산 후 update (프로시저 실행) 
		null as INPUT_PNL_QTY,						-- 투입수량 (PNL) (투입계획). UI에서 직접 입력. (입력 항목 확인) 
		case when A.PNL_QTY != 0 then A.M2_QTY / A.PNL_QTY else 0 end as SQM_PER_PNL_RATIO, 
		-- [INPUT_SQM_QTY][float] NULL,				-- 투입수량 (SQM). UI에서만 표시. = [INPUT_PNL_QTY] * [SQM_PER_PNL_RATIO]   --> 엔진에서 가져가는 값. 계산해서 가져갈 것. 
		case when A.PNL_QTY != 0 then A.QTY_PCS / A.PNL_QTY else 0 end as PCS_PER_PNL_RATIO, 
		-- [INPUT_PCS_QTY][float] NULL,				-- 투입수량 (PCS). UI에서만 표시. = [INPUT_PNL_QTY] * [PCS_PER_PNL_RATIO]
		--A.NEW_START_DATE,  --## UI에서 수정 가능 
		''  as  NEW_START_DATE,  --## UI에서 수정 가능 
		null DESCRIPTION, 
		--A.COMPONENT_ITEM_CODE, A.COMPONENT_ITEM_DESC, 
		-- CCL 유효기한, PPG 유효기한 --> 어떻게 가져오는지? 엑셀 업로드에 자재코드 넣어줄 것. 
		F.MIN_EXPIRATION_DATE as CCL_EXPIRATION_DATE, -- CCL 유효기한
		datediff(DAY, getdate(), F.MIN_EXPIRATION_DATE ) as  CCL_EXPIRATION_DAYS,  
		NULL PPG_EXPIRATION_DATE,		-- PPG 유효기한
		NULL PPG_EXPIRATION_DAYS,
		case when A.BBT_HDI > 0 or A.BBT> 0 then 'Y' else 'N' end BBT_YN,					-- BBT 여부 
		NULL BBT_JIG_CAPA,				-- BBT JIG Capa (KPCS) 
		E.attribute11 as  LOT_SIZE, 
  		A.SHRINKAGE_RATE,
		'' INSERT_ID, '' INSERT_DTTM, '' UPDATE_ID, '' UPDATE_DTTM,
        A.ORDER_DATE,
        LEFT(G.PPG_EXPIRTATION_DATE, 10) AS PPG_EXPIRTATION_DATE,
        G.PPG_EXPIRATION_DAY
from	TH_TAR_READY_BY_INPUT_L_V A  -- select * from TH_TAR_READY_BY_INPUT_L_V
		left outer join
		BOOKED_INFO B 
		on A.SO_NUMBER = B.ORDER_NUMBER 
		left outer join 
		ITEM_REV_LIST C 
		on A.item_code = C.item_code 
		left outer join 
		APS_ITEM_TOT_LT D
		on A.item_code = D.item_code 
		and C.revision = D.revision 
		left outer join 
		mtl_system_items_b E
		on A.ITEM_CODE = E.SEGMENT1
		left outer join 
		MATERIAL_EXPIRATION F
		on A.COMPONENT_ITEM_CODE = F.ITEM_CODE
        left outer join
		PPG_EXPIRATION G
		on A.ITEM_CODE = G.ITEM_CODE
where 
	1=1
");
			

			
            if (dt.Rows.Count > 0) // 이미 데이터가 있으면 TH_TAR_INPUT_PLAN_L 테이블에서 가져옴
            {
				StringBuilder sSQL2 = new StringBuilder();

				sSQL2.Append($@"
select
	VERSION
	,CUST_NAME
	,CUSTOMER_NUMBER
	,SHIP_TO
	,END_CUST
	,BOOKED_FLAG
	,ITEM_CODE
	,MODEL_NAME
	,SO_NUMBER
	,SCHEDULE_LINE_ID
	,SCHEDULE_LINE_SEQ
	,STD_LEAD_TIME
	,ATP_LEAD_TIME
	,CEILING(APS_LEAD_TIME) AS APS_LEAD_TIME
	,STD_LT_INPUT_DATE
	,ATP_LT_INPUT_DATE
	,APS_LT_INPUT_DATE
	,NEW_DATE
	,ORDER_DATE
	,WAITING_DAYS
	,round(READY_BY_INPUT_PNL_QTY, 0) as READY_BY_INPUT_PNL_QTY
	,round(READY_BY_INPUT_SQM_QTY, 0) as READY_BY_INPUT_SQM_QTY
	,READY_BY_INPUT_PCS_QTY
	,format(APS_PLAN_INPUT_DATE, 'yyyy-MM-dd') AS APS_PLAN_INPUT_DATE
    ,APS_PLAN_INPUT_DATE_INPUT_QTY
	,INPUT_PNL_QTY
	,INPUT_PNL_QTY * SQM_PER_PNL_RATIO AS SQM_PER_PNL 
	,INPUT_PNL_QTY * PCS_PER_PNL_RATIO AS PRC_PER_PNL 
	,SQM_PER_PNL_RATIO
	,PCS_PER_PNL_RATIO
	,NEW_START_DATE
	,DESCRIPTION
	,CCL_EXPIRATION_DATE
	,CCL_EXPIRATION_DAYS
	,PPG_EXPIRATION_DATE
	,PPG_EXPIRATION_DAYS
	,BBT_YN
	,BBT_JIG_CAPA
	,LOT_SIZE
	,SHRINKAGE_RATE
from TH_TAR_INPUT_PLAN_L WHERE 1=1 AND VERSION = {terms["VERSION"].V}
order by VERSION, CUST_NAME, ITEM_CODE, NEW_DATE
");
                return Data.Get(sSQL2.ToString()).Tables[0];
            } else // 데이터가 없는 신규버전이면 기본값으로 넣어야함.
			{
                return Data.Get(sSQL.ToString()).Tables[0];
            }
        }

        /// <summary>
        /// 새로운 버전 만들기 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveNewVersion(Params terms)
        {
			StringBuilder sSQL = new StringBuilder();

            string USER_ID = Cookie<User>.Store.USER_ID;

			sSQL.Append($@"
with 
NEW_REV_V as (
	select isnull( (select max(REVISION) from TH_TAR_INPUT_PLAN_DIVISION_H where PLAN_DATE= convert(date, getdate()) AND DIVISION_ID = {terms["group_id"].V}  ), 0) +1 as NEW_REV 
)
insert into [TH_TAR_INPUT_PLAN_DIVISION_H]  
select	
	{terms["group_id"].V} AS DIVISION_ID,
	{terms["group_id"].V} + '_' + format(getdate(), 'yyyyMMdd')+'_'+ format( (select NEW_REV from NEW_REV_V), '000') as  VERSION,  
	convert(date, getdate()) AS PLAN_DATE, 
	(select NEW_REV from NEW_REV_V) as REVISION, 
	(select MAX(VERSION) from TH_TAR_READY_BY_INPUT_H where division_id = {terms["group_id"].V} and use_yn = 'Y') as READY_BY_INPUT_VER, 
	null APS_PLAN_ID, -- UI에서 선택한 PLAN ID 를 입력 
	(select  CUTOFF_DATE from TH_TAR_MATERIAL_INVENTORY_H where use_yn = 'Y' ) as MAT_INVEN_CUTOFF_DATE,  
	(select  REVISION    from TH_TAR_MATERIAL_INVENTORY_H where use_yn = 'Y' ) as MAT_INVEN_CUTOFF_REVISION,  
	'Y' as USE_YN, 
	null as DESCRIPTION, 
	'{USER_ID}' INSERT_ID, 
	getdate() INSERT_DTTM, 
	'{USER_ID}' UPDATE_ID,
	getdate() UPDATE_DTTM
");

            StringBuilder sSQL2 = new StringBuilder();

			sSQL2.Append($@"
UPDATE TH_TAR_INPUT_PLAN_DIVISION_H SET USE_YN = 'N' WHERE DIVISION_ID = {terms["group_id"].V}
");


            // 첫번째 생성되면 첫본 자동으로 저장
            StringBuilder sSQL3 = new StringBuilder();

            sSQL3.Append(@"
-- 버전이 SPS, HDI 두 가지로 업로드 될 것. 
with 
BOOKED_INFO as (
	select * from openquery(ERP_RUN,   'select  HEADER_ID, ORG_ID, ORDER_TYPE_ID, ORDER_NUMBER, VERSION_NUMBER, ORDERED_DATE, CANCELLED_FLAG, BOOKED_FLAG, BOOKED_DATE, FLOW_STATUS_CODE 
										from    oe_order_headers_all where ORDERED_DATE > = sysdate - 365')  -- 기본 1년치 조회 
	--order by ordered_Date 
),
--SPS 버전, HDI 버전 --## UI에서 선택 또는 최신 버전 
TH_TAR_READY_BY_INPUT_L_V as (
	select	*
	from	TH_TAR_READY_BY_INPUT_L A 
	where	
            1=1
            AND ITEM_CODE IS NOT NULL
");
            if (terms["group_id"].Length > 0)
            {
                if (terms["group_id"].AsString() == "SPS")
                {
                    sSQL3.Append($@"
            AND A.PROD_GROUP = 'S' 
");
                }
                else if (terms["group_id"].AsString() == "HDI")
                {
                    sSQL3.Append($@"
            AND A.PROD_GROUP = 'H' 
");
                }
            }


            sSQL3.Append($@"
) 
,
ITEM_REV_LIST as (
	select	DISTINCT A.ITEM_CODE, (select max(B.revision) from cbst_spec_basic B where B.item_code = A.item_code) as REVISION
	from	TH_TAR_READY_BY_INPUT_L_V A 
	--order by A.ITEM_CODE, REVISION
)
--검증 
--select	A.*, B.ITEM_CODE, B.REVISION
--from	ITEM_REV_LIST A
--		left outer join 
--		--th_tar_routing_h B
--		(
--			select B.ITEM_CODE, B.REVISION from th_tar_routing_L B group by  B.ITEM_CODE, B.REVISION
--		) B
--		on a.ITEM_CODE=b.item_code
--		and a.revision = b.revision
--order by A.ITEM_CODE, A.REVISION
--;
,
APS_LT as (
	select  A.ITEM_CODE, A.REVISION, 
			--B.*, 
			B.OPERATION_SEQ, B.DEPARTMENT_CODE, --B.DEPARTMENT_NAME, 
			C.PLAN_PROCESSING_TIME_HR, C.PLAN_ESSENTIAL_WAITING_TIME_HR, PLAN_OPER_WAITING_TIME_HR, 
			isnull(C.PLAN_PROCESSING_TIME_HR, 0) + isnull(C.PLAN_ESSENTIAL_WAITING_TIME_HR, 0) + isnull(PLAN_OPER_WAITING_TIME_HR, 0) as ITEM_ROUTE_LT_HR
	from	ITEM_REV_LIST A
			left outer join 
			th_tar_routing_L B  -- select top 10 * from th_tar_routing_L where  use_yn = 'Y'
			on  A.item_code = B.item_code 
			and A.revision = B.revision
			and B.DEPARTMENT_ID > 0 
			--and B.USE_YN = 'Y'  --## 2025.08.18. 빼야 함 
			left outer join 
			TH_TAR_PLAN_DEPT_LEADTIME C  --  select top 10 * from TH_TAR_PLAN_DEPT_LEADTIME
			on b.organization_id = C.ORGANIZATION_ID
			and B.DEPARTMENT_CODE = C.DEPARTMENT_CODE
	where 1=1
	--and A.item_Code = 'BOC00023A00'--  'MCP23369A00'  -- 임시 
	--and B.use_yn = 'Y'
)
--select * from APS_LT
,
APS_ITEM_TOT_LT as (
	select item_code, revision, sum(ITEM_ROUTE_LT_HR)/24 as ITEM_APS_LT_DAY
	from APS_LT 
	group by item_code, revision
), 
MATERIAL_EXPIRATION as (
	select	A.INVENTORY_ITEM_ID, A.ITEM_CODE, A.MIN_EXPIRATION_DATE  
	from	TH_TAR_MATERIAL_INVENTORY_L A  -- select * from  TH_TAR_MATERIAL_INVENTORY_L  -- CUTOFF_DATE, SEQ, REVISION, INVENTORY_ITEM_ID, ITEM_CODE, MIN_EXPIRATION_DATE 
			inner join 
			TH_TAR_MATERIAL_INVENTORY_H B -- select * from  TH_TAR_MATERIAL_INVENTORY_H
			on A.CUTOFF_DATE = B.CUTOFF_DATE
			and		A.REVISION = B.REVISION 
	where B.use_yn = 'Y'   -- 현재 유효 버전 적용  --> 투입계획 Header에 이 버전을 입력해줄 것 
)
--,
--BBT_CAPA as (
---- ERP12 BBT JIG 현황 : STC MFG > BBT Jig Inquery  -- 운영 접근 안됨 
--		select * from openquery(ERP_RUN,   '
--				SELECT jig_code, capa_pcs
--				FROM   cbwip_bbt_jig_list_v
--				WHERE  organization_id = 101
--				AND    (category_nm = ''S'')
--		')
--) 
--select * from BBT_CAPA
--select * from APS_ITEM_TOT_LT;
--insert into TH_TAR_INPUT_PLAN_L
-- (VERSION, CUST_NAME, CUSTOMER_NUMBER, SHIP_TO, END_CUST, BOOKED_FLAG, ITEM_CODE, MODEL_NAME, SO_NUMBER, SCHEDULE_LINE_ID, SCHEDULE_LINE_SEQ, 
--		STD_LEAD_TIME, ATP_LEAD_TIME, APS_LEAD_TIME, STD_LT_INPUT_DATE, ATP_LT_INPUT_DATE, APS_LT_INPUT_DATE, NEW_DATE, ORDER_DATE, WAITING_DAYS, 
--		READY_BY_INPUT_PNL_QTY, READY_BY_INPUT_SQM_QTY, READY_BY_INPUT_PCS_QTY, APS_PLAN_INPUT_DATE, INPUT_PNL_QTY, SQM_PER_PNL_RATIO, PCS_PER_PNL_RATIO, NEW_START_DATE, DESCRIPTION, 
--		CCL_EXPIRATION_DATE, CCL_EXPIRATION_DAYS, PPG_EXPIRATION_DATE, PPG_EXPIRATION_DAYS, BBT_YN, BBT_JIG_CAPA, LOT_SIZE, SHRINKAGE_RATE, INSERT_ID, INSERT_DTTM, UPDATE_ID, UPDATE_DTTM
--		)
select	A.VERSION, 
        A.CUST_NAME, 
		(select max(B.customer) from cbst_spec_basic  B where B.ITEM_CODE = A.ITEM_CODE ) as CUSTOMER_NUMBER, -- 쓸 일이 있을지?  
		null SHIP_TO, null END_CUST, B.BOOKED_FLAG, 
		A.ITEM_CODE, A.MODEL_NAME, 
		A.SO_NUMBER, 
		A.SCHEDULE_LINE_ID, 
		1 as SCHEDULE_LINE_SEQ, -- 초기값은 1, 분할하게 되면 행 추가하면서 1 증가
		A.LEAD_TIME as STD_LEAD_TIME, null ATP_LEAD_TIME, 
		D.ITEM_APS_LT_DAY as APS_LEAD_TIME, 
		A.NEW_DATE - A.LEAD_TIME as STD_LT_INPUT_DATE, null ATP_LT_INPUT_DATE, 
		A.NEW_DATE - D.ITEM_APS_LT_DAY as APS_LT_INPUT_DATE, --A.PROD_GROUP, A.CCL_QTY, A.FLM, A.MFG_CATEGORY, A.LAYER, 
		--A.LAYUP_DESC,   A.MODEL_NAME, A.LEAD_TIME, 
		--A.NEW_START_DATE, 
		A.NEW_DATE, -- 납기 
		A.ORDER_DATE, -- UI 미표시 
		datediff(DAY, getdate(), A.ORDER_DATE) as WAITING_DAYS,
		A.PNL_QTY as READY_BY_INPUT_PNL_QTY, A.M2_QTY as READY_BY_INPUT_SQM_QTY, A.QTY_PCS as READY_BY_INPUT_PCS_QTY, 
		null as APS_PLAN_INPUT_DATE,				-- APS 투입계획일자. Plan ID 가지고 별도 계산 후 update (프로시저 실행) 
		null as INPUT_PNL_QTY,						-- 투입수량 (PNL) (투입계획). UI에서 직접 입력. (입력 항목 확인) 
		case when A.PNL_QTY != 0 then A.M2_QTY / A.PNL_QTY else 0 end as SQM_PER_PNL_RATIO, 
		-- [INPUT_SQM_QTY][float] NULL,				-- 투입수량 (SQM). UI에서만 표시. = [INPUT_PNL_QTY] * [SQM_PER_PNL_RATIO]   --> 엔진에서 가져가는 값. 계산해서 가져갈 것. 
		case when A.PNL_QTY != 0 then A.QTY_PCS / A.PNL_QTY else 0 end as PCS_PER_PNL_RATIO, 
		-- [INPUT_PCS_QTY][float] NULL,				-- 투입수량 (PCS). UI에서만 표시. = [INPUT_PNL_QTY] * [PCS_PER_PNL_RATIO]
		--A.NEW_START_DATE,  --## UI에서 수정 가능 
		''  as  NEW_START_DATE,  --## UI에서 수정 가능 
		null DESCRIPTION, 
		--A.COMPONENT_ITEM_CODE, A.COMPONENT_ITEM_DESC, 
		-- CCL 유효기한, PPG 유효기한 --> 어떻게 가져오는지? 엑셀 업로드에 자재코드 넣어줄 것. 
		F.MIN_EXPIRATION_DATE as CCL_EXPIRATION_DATE, -- CCL 유효기한
		datediff(DAY, getdate(), F.MIN_EXPIRATION_DATE ) as  CCL_EXPIRATION_DAYS,  
		NULL PPG_EXPIRATION_DATE,		-- PPG 유효기한
		NULL PPG_EXPIRATION_DAYS,
		case when A.BBT_HDI > 0 or A.BBT> 0 then 'Y' else 'N' end BBT_YN,					-- BBT 여부 
		NULL BBT_JIG_CAPA,				-- BBT JIG Capa (KPCS) 
		E.attribute11 as  LOT_SIZE, 
  		A.SHRINKAGE_RATE,
		'' INSERT_ID, '' INSERT_DTTM, '' UPDATE_ID, '' UPDATE_DTTM
from	TH_TAR_READY_BY_INPUT_L_V A  -- select * from TH_TAR_READY_BY_INPUT_L_V
		left outer join
		BOOKED_INFO B 
		on A.SO_NUMBER = B.ORDER_NUMBER 
		left outer join 
		ITEM_REV_LIST C 
		on A.item_code = C.item_code 
		left outer join 
		APS_ITEM_TOT_LT D
		on A.item_code = D.item_code 
		and C.revision = D.revision 
		left outer join 
		mtl_system_items_b E
		on A.ITEM_CODE = E.SEGMENT1
		left outer join 
		MATERIAL_EXPIRATION F
		on A.COMPONENT_ITEM_CODE = F.ITEM_CODE
where 
	1=1
");





            try
            {
                // 첫 번째 SQL 실행 USE_YN을 먼저 N으로 변경한 후에 생성된 버전만 Y로 남겨둬야함
                HS.Web.Common.Data.Execute(sSQL2.ToString());
                // 버전 추가
                HS.Web.Common.Data.Execute(sSQL.ToString());
                // 버전에 해당하는 데이터 리스트 저장
                //HS.Web.Common.Data.Execute(sSQL3.ToString());
            }
            catch (Exception ex)
            {
                // 오류 발생 시 처리
                Console.WriteLine("오류 발생: " + ex.Message);
            }

        }

        private bool updatePlanId(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
UPDATE TH_TAR_INPUT_PLAN_DIVISION_H SET APS_PLAN_ID = {terms["plan_id"].V} WHERE VERSION = {terms["INPUT_PLAN_VERSION"].V}
");

            try
            {
                HS.Web.Common.Data.Execute(sSQL.ToString());
                return true;
            } catch(Exception e)
            {
                Console.WriteLine("오류 발생: " + e.Message);
                return false;
            }

            
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data, string version)
        {
            //string version = version.ToString();
            // TODO : SO, LINE 들어오기전까지만 들어오고 난 후에는 UPDATE 로직 추가 필요.
            // 트랜잭션으로 같은 버전 데이터 삭제 후 새로 집어넣기

            var table = new DataTable();
            table.Columns.Add("VERSION", typeof(string));
            table.Columns.Add("CUST_NAME", typeof(string));
            table.Columns.Add("CUSTOMER_NUMBER", typeof(int));
            table.Columns.Add("SHIP_TO", typeof(string));
            table.Columns.Add("END_CUST", typeof(string));
            table.Columns.Add("BOOKED_FLAG", typeof(string));
            table.Columns.Add("ITEM_CODE", typeof(string));
            table.Columns.Add("MODEL_NAME", typeof(string));
            table.Columns.Add("SO_NUMBER", typeof(string));
            table.Columns.Add("SCHEDULE_LINE_ID", typeof(string));
            table.Columns.Add("SCHEDULE_LINE_SEQ", typeof(int));
            table.Columns.Add("STD_LEAD_TIME", typeof(double));
            table.Columns.Add("ATP_LEAD_TIME", typeof(double));
            table.Columns.Add("APS_LEAD_TIME", typeof(double));
            table.Columns.Add("STD_LT_INPUT_DATE", typeof(DateTime));
            table.Columns.Add("ATP_LT_INPUT_DATE", typeof(DateTime));
            table.Columns.Add("APS_LT_INPUT_DATE", typeof(DateTime));
            table.Columns.Add("NEW_DATE", typeof(DateTime));
            table.Columns.Add("ORDER_DATE", typeof(DateTime));
            table.Columns.Add("WAITING_DAYS", typeof(double));
            table.Columns.Add("READY_BY_INPUT_PNL_QTY", typeof(double));
            table.Columns.Add("READY_BY_INPUT_SQM_QTY", typeof(double));
            table.Columns.Add("READY_BY_INPUT_PCS_QTY", typeof(double));
            table.Columns.Add("APS_PLAN_INPUT_DATE", typeof(DateTime));
            table.Columns.Add("INPUT_PNL_QTY", typeof(double));
            table.Columns.Add("SQM_PER_PNL_RATIO", typeof(double));
            table.Columns.Add("PCS_PER_PNL_RATIO", typeof(double));
            table.Columns.Add("NEW_START_DATE", typeof(DateTime));
            table.Columns.Add("DESCRIPTION", typeof(string));
            table.Columns.Add("CCL_EXPIRATION_DATE", typeof(DateTime));
            table.Columns.Add("CCL_EXPIRATION_DAYS", typeof(int));
            table.Columns.Add("PPG_EXPIRATION_DATE", typeof(DateTime));
            table.Columns.Add("PPG_EXPIRATION_DAYS", typeof(int));
            table.Columns.Add("BBT_YN", typeof(string));
            table.Columns.Add("BBT_JIG_CAPA", typeof(double));
            table.Columns.Add("LOT_SIZE", typeof(double));
            table.Columns.Add("SHRINKAGE_RATE", typeof(double));
            table.Columns.Add("INSERT_ID", typeof(string));
            table.Columns.Add("INSERT_DTTM", typeof(DateTime));
            table.Columns.Add("UPDATE_ID", typeof(string));
            table.Columns.Add("UPDATE_DTTM", typeof(DateTime));

            

            string USER_ID = Cookie<User>.Store.USER_ID;

            foreach (var item in data)
            {

                // 실제 삽입
                table.Rows.Add(
                    version,
                    SafeValue(item["CUST_NAME"].AsString(), typeof(string)),
                    SafeValue(item["CUSTOMER_NUMBER"].AsString(), typeof(int)),
                    SafeValue(item["SHIP_TO"].AsString(), typeof(string)),
                    SafeValue(item["END_CUST"].AsString(), typeof(string)),
                    SafeValue(item["BOOKED_FLAG"].AsString(), typeof(string)),
                    SafeValue(item["ITEM_CODE"].AsString(), typeof(string)),
                    SafeValue(item["MODEL_NAME"].AsString(), typeof(string)),
                    SafeValue(item["SO_NUMBER"].AsString(), typeof(string)),
                    SafeValue(item["SCHEDULE_LINE_ID"].AsString(), typeof(string)),
                    SafeValue(item["SCHEDULE_LINE_SEQ"].AsString(), typeof(int)),
                    SafeValue(item["STD_LEAD_TIME"].AsString(), typeof(double)),
                    SafeValue(item["ATP_LEAD_TIME"].AsString(), typeof(double)),
                    SafeValue(item["APS_LEAD_TIME"].AsString(), typeof(double)),
                    SafeValue(item["STD_LT_INPUT_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["ATP_LT_INPUT_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["APS_LT_INPUT_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["NEW_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["ORDER_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["WAITING_DAYS"].AsString(), typeof(double)),
                    SafeValue(item["READY_BY_INPUT_PNL_QTY"].AsString(), typeof(double)),
                    SafeValue(item["READY_BY_INPUT_SQM_QTY"].AsString(), typeof(double)),
                    SafeValue(item["READY_BY_INPUT_PCS_QTY"].AsString(), typeof(double)),
                    SafeValue(item["APS_PLAN_INPUT_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["INPUT_PNL_QTY"].AsString(), typeof(double)),
                    SafeValue(item["SQM_PER_PNL_RATIO"].AsString(), typeof(double)),
                    SafeValue(item["PCS_PER_PNL_RATIO"].AsString(), typeof(double)),
                    SafeValue(item["NEW_START_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["DESCRIPTION"].AsString(), typeof(string)),
                    SafeValue(item["CCL_EXPIRATION_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["CCL_EXPIRATION_DAYS"].AsString(), typeof(int)),
                    SafeValue(item["PPG_EXPIRATION_DATE"].AsString(), typeof(DateTime)),
                    SafeValue(item["PPG_EXPIRATION_DAYS"].AsString(), typeof(int)),
                    SafeValue(item["BBT_YN"].AsString(), typeof(string)),
                    SafeValue(item["BBT_JIG_CAPA"].AsString(), typeof(double)),
                    SafeValue(item["LOT_SIZE"].AsString(), typeof(double)),
                    SafeValue(item["SHRINKAGE_RATE"].AsString(), typeof(double)),
                    USER_ID,
                    DateTime.Now,
                    USER_ID,
                    DateTime.Now
                );


            }



            (string dbType, string connection) = Data.GetConnection("Default");

			using (SqlConnection conn = new SqlConnection(connection))
			{
				conn.Open();
				using (SqlTransaction tx = conn.BeginTransaction())
				{
					try
					{

                        StringBuilder sSQL = new StringBuilder();
                        //  선택받은 DIVISION에 해당하는것만 삭제하고 다시 올려야함.
                        if (version != null)
                        {
                            sSQL.Append($@"
DELETE FROM TH_TAR_INPUT_PLAN_L WHERE 1=1 AND VERSION = '{version}'
");
                        }
                        
						

                        using (SqlCommand deleteCmd = new SqlCommand(sSQL.ToString(), conn, tx))
                        {
                            HS.Web.Common.Data.Execute(sSQL.ToString());
                        }

                        // bulk
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx))
                        {
                            bulkCopy.DestinationTableName = "TH_TAR_INPUT_PLAN_L";

                            // ColumnMappings 설정
                            // 컬럼 리스트
                            string[] columns = new string[]
                            {
								"VERSION",
                                "CUST_NAME",
                                "CUSTOMER_NUMBER",
                                "SHIP_TO", "END_CUST", "BOOKED_FLAG",
                                "ITEM_CODE", "MODEL_NAME", "SO_NUMBER", "SCHEDULE_LINE_ID", "SCHEDULE_LINE_SEQ",
                                "STD_LEAD_TIME", "ATP_LEAD_TIME", "APS_LEAD_TIME",
                                "STD_LT_INPUT_DATE",
                                "ATP_LT_INPUT_DATE", "APS_LT_INPUT_DATE",
                                "NEW_DATE", "ORDER_DATE", "WAITING_DAYS",
                                "READY_BY_INPUT_PNL_QTY", "READY_BY_INPUT_SQM_QTY", "READY_BY_INPUT_PCS_QTY",
                                "APS_PLAN_INPUT_DATE", "INPUT_PNL_QTY", "SQM_PER_PNL_RATIO", "PCS_PER_PNL_RATIO",
                                "NEW_START_DATE", "DESCRIPTION", "CCL_EXPIRATION_DATE", "CCL_EXPIRATION_DAYS",
                                "PPG_EXPIRATION_DATE", "PPG_EXPIRATION_DAYS", "BBT_YN", "BBT_JIG_CAPA", "LOT_SIZE",
                                "SHRINKAGE_RATE",
                                "INSERT_ID", "INSERT_DTTM", "UPDATE_ID", "UPDATE_DTTM"
                            };

                            // ColumnMappings 자동 설정
                            foreach (var col in columns)
                            {
                                bulkCopy.ColumnMappings.Add(col, col);
                            }

                            // 데이터 삽입
                            bulkCopy.WriteToServer(table);
                        }

                        //HS.Web.Proc.TH_TAR_INPUT_PLAN_L.Save(data, version);

                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        private bool ApsInputDateUpdate(Params data)
        {
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"     
                    DECLARE @p_INPUT_PLAN_VERSION VARCHAR(150) ;
                    DECLARE @p_APS_PLAN_ID VARCHAR(150) ;
                    SET @p_INPUT_PLAN_VERSION  = {data["INPUT_PLAN_VERSION"].V};
                    SET @p_APS_PLAN_ID = {data["APS_PLAN_ID"].V};
                    EXEC dbo.PR_INPUT_PLAN_APS_INPUT_DATE_UPDATE @p_INPUT_PLAN_VERSION, @p_APS_PLAN_ID;
                    SELECT 'OK'AS RTN ;
                    ");
            try
            {
                DataTable dt = Data.Get(sSQL.ToString()).Tables[0];
                return true; // 성공
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.ToString());
                return false; // 실패
            }
        }

        public static object SafeValue(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DBNull.Value;

            if (targetType == typeof(int) && int.TryParse(value, out var i)) return i;
            if (targetType == typeof(double) && double.TryParse(value, out var d)) return d;
            if (targetType == typeof(DateTime) && DateTime.TryParse(value, out var dt)) return dt;

            return value; // 기본은 string
        }


        private DataTable Searchinputstock(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            var sSQL = new StringBuilder();
            sSQL.Append($@" 
                SELECT 
                        MATERIAL_ITEM_CODE,
                        MATERIAL_ITEM_NAME,
                        REQ_MATERIAL_QTY
                 FROM  [dbo].[TH_TAR_OM_ITEM_REQ_MATERIAL] with (nolock) 
                WHERE ORDER_DATE = FORMAT(GETDATE(), 'yyyyMMdd') -- 현재일  '20250923'
                  --AND ITEM_CODE = 'BOC04640A00'  
                   AND ITEM_CODE = {terms["ITEM_CODE"].V}  -- 선택한 행의 ITEM CODE 


            ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }

    }
}
