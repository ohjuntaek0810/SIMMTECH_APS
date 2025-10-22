using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Spreadsheet;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class delivery_result_calc : BasePageModel
    {
        public delivery_result_calc()
        {
            this.Handler = handler;
            Params result = HS.Web.Common.ApsManage.searchPlanId().ToParams();
          //  Console.WriteLine(result["PLAN_ID"].AsString());
            this.Params["first_plan_id"] = result["PLAN_ID"];
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params searchterms = e.Params["searchterms"];
                toClient["MasterData"] = this.SearchMaster(searchterms);
                return toClient;
            }

            if (e.Command == "search_detail")
            {
                Params rowterms = e.Params["rowterms"];      
                toClient["DetailData"] = this.SearchDetail(rowterms);
                return toClient;
            }

            if (e.Command == "saveMaster")
            {
                Params searchterms = e.Params["searchterms"];
                ParamList dataList = e.Params["data"];
                ValiList vali = new ValiList(dataList);
                vali.Null("MODEL_NAME", "Item 정보는 필수 입력 사항입니다.");
                vali.Null("REVISION", "Item 정보는 필수 입력 사항입니다.");
                vali.Null("ORDER_DATE", "ORDER_DATE 가 입력되지 않았습니다.");
                vali.Null("DIVISION_ID", "DIVISION_ID 가 입력되지 않았습니다.");
                vali.DoneDeco();
                this.SavePROMISING(dataList);
                toClient["MasterData"] = this.SearchMaster(searchterms);
                return toClient;                
            }
            if (e.Command == "delete")
            {
                Params searchterms = e.Params["searchterms"];
                ParamList dataList = e.Params["data"];
                this.deletePROMISING(dataList);
                toClient["MasterData"] = this.SearchMaster(searchterms);
                return toClient;
            }

            if (e.Command == "DT_OM_BOTTLE_NECK_LIST_V")
            {   

                toClient["OM_BOTTLE_NECK_LIST_V_RESULT"] = this.DT_OM_BOTTLE_NECK_LIST_V();

                return toClient;
            }

            if (e.Command == "DT_OM_BOTTLE_NECK_LIST_V")
            {

                toClient["OM_BOTTLE_NECK_LIST_V_RESULT"] = this.DT_OM_BOTTLE_NECK_LIST_V();

                return toClient;
            }

            if (e.Command == "EXEC_PR_OM_DAILY_CAPA_CALC")
            {
                Params Terms = e.Params["Terms"];

                Vali vali = new Vali(Terms);
                vali.Null("PLAN_ID", "PLAN_ID 가 없습니다.");              
                vali.DoneDeco();


                toClient["RESULT_PARAMS"] = HS.Web.Common.ApsManage.EXEC_PR_OM_DAILY_CAPA_CALC(Terms).ToParams();

                return toClient;
            }

            if (e.Command == "PR_OM_MATERIAL_SIMUL")
            {
                Params Terms = e.Params["Terms"];

                toClient["result"] = this.PR_OM_MATERIAL_SIMUL();
            }


            return toClient;
        }

        private bool PR_OM_MATERIAL_SIMUL()
        {
            (string dbType, string connection) = Data.GetConnection("Default");

            try
            {
                using SqlConnection conn = new SqlConnection(connection);
                conn.Open();
                using var cmd = new SqlCommand("PR_OM_MATERIAL_SIMUL", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
                return true; // 성공
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.ToString());
                return false; // 실패
            }
        }

        private DataTable DT_OM_BOTTLE_NECK_LIST_V()
        {
            
            var sSQL1 = new StringBuilder();

            sSQL1.Append($@" SELECT BOTTLE_NECK_ID,BOTTLE_NECK_NAME FROM OM_BOTTLE_NECK_LIST_V WITH (NOLOCK) ORDER BY SORT_ORDER ");

            DataTable dt = Data.Get(sSQL1.ToString()).Tables[0];
            return dt;
        }


		private DataTable SearchMaster(Params terms)
		{
			DTClient.UserInfoMerge(terms);
            var sSQL = new StringBuilder();

            DataTable dt = DT_OM_BOTTLE_NECK_LIST_V();
         
            sSQL.Append($@"
    WITH 
    ITEM_DIVISION AS (
                SELECT
                ITEM_CODE,
                REVISION,
                DIVISION_ID,
");
            foreach (DataRow row in dt.Rows)
            {
                // 각 행에 대해 작업 수행
                sSQL.Append($@" MAX(CASE WHEN BOTTLE_NECK_NAME = '" + row["BOTTLE_NECK_NAME"] + @"' THEN 'Y' ELSE NULL END) AS [" + row["BOTTLE_NECK_NAME"] + @"],
                ");
            }

            sSQL.Append($@"
            1 as a
            FROM (
                SELECT 
                    D.ITEM_CODE,
                    D.REVISION,
                    D.DIVISION_ID,
                    ID.BOTTLE_NECK_NAME
                FROM TH_TAR_OM_ORDER_PROMISING D 
                LEFT OUTER JOIN TH_TAR_RCG_ITEM_DIVISION ID   
                    ON D.ITEM_CODE = ID.ITEM_CODE 
                    AND D.REVISION = ID.REVISION 
                    AND D.DIVISION_ID = ID.DIVISION_ID  
       
       
            ) AS SourceTable
            GROUP BY 
                ITEM_CODE,
                REVISION,
                DIVISION_ID
            )
            SELECT    
                    M.ORDER_DATE,
                    M.TEAM,
                    M.CUST_ID,		-- UI 미표시. DB 저장.  
                    M.CUST_NAME,
                    M.SHIP_TO_ID,	-- UI 미표시. DB 저장.  
                    M.SHIP_TO_NAME,
                    M.DIVISION_ID,	-- UI 미표시. 필터 조건.  
                    M.MODEL_NAME,
                    M.PO_NUMBER,
                    M.CUST_PO_NUMBER,  -- UI 미표시.
			        M.REQUEST_ORDER_ID,			-- 납기요청 순번
                    M.REQUEST_ORDER_SORT_ORDER, -- UI 미표시. 납기요청 행 표시 순서1
                    D.REQUEST_ORDER_DUE_SEQ,	-- 납기일 순번 (일자순 아님, 사용자가 정한 순서) 
                    D.REQUEST_ORDER_DUE_SEQ_ORDER, -- UI 미표시. 납기요청별 납기일 행 표시 순서2
                    D.TOTAL_SORT_ORDER, -- UI 미표시
                    M.ITEM_CODE,
                    M.REVISION,		    -- UI 미표시.
                    M.TOTAL_QTY_PCS,	-- 요청 총 수량 (PCS)
			        D.QTY_PCS,			-- 납기일별 수량 (PCS) 
			        case when convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) > 0 
				            then round(1.0*M.TOTAL_QTY_PCS/( convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) ) * convert(float, C.PANEL_SIZE_X) * convert(float, C.PANEL_SIZE_Y)/1000000, 2)  
				            else null end 
			        as TOTAL_QTY_SQM,	-- 요청 총 수량 (SQM)
			        case when convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) > 0 
				            then round(1.0*D.QTY_PCS/( convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) ) * convert(float, C.PANEL_SIZE_X) * convert(float, C.PANEL_SIZE_Y)/1000000, 2)  
				            else null end 
			        as QTY_SQM,			-- 납기일별 수량 (SQM)
			        D.PROMISING_DATE,	-- 납기일
			        -- M.PROMISE_DATE,  -- 미사용
                    LT.ITEM_LT_DAY as LeadTime, -- Lead Time 
			        (1.0-isnull(convert(float, IPG.SHRINKAGE_RATE), 0.0)) as yield, -- 수율
                    M.DESCRIPTION AS M_DESCRIPTION, -- UI 미표시.
                    M.REQUEST_DATE,			-- 요청일
                    D.DESCRIPTION  AS D_DESCRIPTION,   -- Remark, 입력 수정 가능
                    FLOOR(JC.TOT_JIG_CAPA_PCS_with_YIELD) AS BBT_CAPA,
			        M.SCHEDULE_SHIP_DATE,	-- UI 미표시.
                    M.SHIP_DATE,			-- UI 미표시.            
                   ");
                    foreach (DataRow row in dt.Rows)
                    {
                        // 각 행에 대해 작업 수행
                     
                        sSQL.Append($@"  ITEM_DIVISION.[" + row["BOTTLE_NECK_NAME"] + @"] AS [" + row["BOTTLE_NECK_NAME"] + @"], ");
                     }
    sSQL.Append($@"    
                    1 as a
                FROM TH_TAR_OM_ORDER_REQUEST M
                LEFT OUTER JOIN TH_TAR_OM_ORDER_PROMISING D 
                ON M.DIVISION_ID = D.DIVISION_ID
                AND M.ORDER_DATE = D.ORDER_DATE
                AND M.REQUEST_ORDER_ID = D.REQUEST_ORDER_ID
                LEFT OUTER JOIN ITEM_DIVISION  
                ON D.ITEM_CODE = ITEM_DIVISION.ITEM_CODE 
                AND D.REVISION = ITEM_DIVISION.REVISION
                AND D.DIVISION_ID = ITEM_DIVISION.DIVISION_ID
	            LEFT OUTER JOIN 
	            TH_GUI_ITEM_MODEL_SEARCH C WITH (NOLOCK)
	            ON  M.ITEM_CODE = C.ITEM_CODE
	            AND M.REVISION = FORMAT(CONVERT(INT,C.REV), '000')  -- Rev 안 들어있을 수 있음 
	            left outer join 
	            TH_TAR_OM_ITEM_LEADTIME LT  -- OM Lead Time -- select * from  TH_TAR_OM_ITEM_LEADTIME with (nolock) where USE_YN = 'Y' and item_code = 'BOC00994A00' 
	            on M.DIVISION_ID = LT.DIVISION_ID  
	            AND M.ITEM_CODE = LT.ITEM_CODE 
	            and LT.USE_YN = 'Y' 
	            left outer join
	            TH_GUI_ITEM_BY_PROCESS_GUBUN IPG with (nolock)
	            on M.item_code = IPG.item_code 
	            and M.revision = FORMAT(CONVERT(INT,IPG.revision), '000')  
	            left outer join
                TH_TAR_JIG_CAPA JC
                on M.item_code = JC.item_code
	            where 
                    D.order_date = {terms["search_order_date"].V.Replace("-", "")} --  UI 조회필터 선택 Order datea 
	            order by M.ORDER_DATE, M.REQUEST_ORDER_SORT_ORDER, -- UI 미표시. 납기요청 행 표시 순서1
                        D.REQUEST_ORDER_DUE_SEQ_ORDER -- UI 미표시. 납기요청별 납기일 행 표시 순서2

        ");


            //Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchMaster백업250911(Params terms)
        {
            DTClient.UserInfoMerge(terms);


            DataTable dt = DT_OM_BOTTLE_NECK_LIST_V(); ;


            foreach (DataRow row in dt.Rows)
            {
                // 각 행에 대해 작업 수행
                Console.WriteLine(row["BOTTLE_NECK_NAME"]); // 예: row["Name"]
            }

            var sSQL = new StringBuilder();

            sSQL.Append($@"
    WITH 
    ITEM_DIVISION AS (
        SELECT
        ITEM_CODE,
        REVISION,
        DIVISION_ID,
");
            foreach (DataRow row in dt.Rows)
            {
                // 각 행에 대해 작업 수행
                sSQL.Append($@" MAX(CASE WHEN BOTTLE_NECK_NAME = '" + row["BOTTLE_NECK_NAME"] + @"' THEN 'Y' ELSE NULL END) AS [" + row["BOTTLE_NECK_NAME"] + @"],");
            }

            sSQL.Append($@"
    WITH 
    ITEM_DIVISION AS (
        SELECT
        ITEM_CODE,
        REVISION,
        DIVISION_ID,
");
            foreach (DataRow row in dt.Rows)
            {
                // 각 행에 대해 작업 수행
                sSQL.Append($@" MAX(CASE WHEN BOTTLE_NECK_NAME = '"+row["BOTTLE_NECK_NAME"]+@"' THEN 'Y' ELSE NULL END) AS ["+row["BOTTLE_NECK_NAME"]+@"],
                ");
            }

            sSQL.Append($@"
 1 as a
    FROM (
        SELECT 
            D.ITEM_CODE,
            D.REVISION,
            D.DIVISION_ID,
            ID.BOTTLE_NECK_NAME
        FROM TH_TAR_OM_ORDER_PROMISING D 
        LEFT OUTER JOIN TH_TAR_RCG_ITEM_DIVISION ID   
            ON D.ITEM_CODE = ID.ITEM_CODE 
            AND D.REVISION = ID.REVISION 
            AND D.DIVISION_ID = ID.DIVISION_ID  
       
       
    ) AS SourceTable
    GROUP BY 
        ITEM_CODE,
        REVISION,
        DIVISION_ID
    )
    SELECT    
            M.ORDER_DATE,
            M.DIVISION_ID,
            M.REQUEST_ORDER_ID,
            M.REQUEST_ORDER_SORT_ORDER,
            M.ITEM_CODE,
            M.MODEL_NAME,
            M.REVISION,
            M.TOTAL_QTY_PCS,
            M.DESCRIPTION AS M_DESCRIPTION,
            M.TEAM,
            M.CUST_ID,
            M.CUST_NAME,
            M.SHIP_TO_ID,
            M.SHIP_TO_NAME,
            M.PO_NUMBER,
            M.CUST_PO_NUMBER,
            M.SCHEDULE_SHIP_DATE,
            M.REQUEST_DATE,
            M.PROMISE_DATE,
            M.SHIP_DATE,                
            D.REQUEST_ORDER_DUE_SEQ,
            D.REQUEST_ORDER_DUE_SEQ_ORDER,
            D.TOTAL_SORT_ORDER,                
            D.PROMISING_DATE,
            D.QTY_PCS,
            D.DESCRIPTION  AS D_DESCRIPTION,
            ");
            foreach (DataRow row in dt.Rows)
            {
                // 각 행에 대해 작업 수행
                sSQL.Append($@"  ITEM_DIVISION.[" + row["BOTTLE_NECK_NAME"] + @"] AS [" + row["BOTTLE_NECK_NAME"] + @"],
                ");
            }
           

    sSQL.Append($@"
            1 as a
     FROM TH_TAR_OM_ORDER_REQUEST M
     LEFT OUTER JOIN TH_TAR_OM_ORDER_PROMISING D 
      ON M.DIVISION_ID = D.DIVISION_ID
     AND M.ORDER_DATE = D.ORDER_DATE
     AND M.REQUEST_ORDER_ID = D.REQUEST_ORDER_ID
    LEFT OUTER JOIN ITEM_DIVISION  
      ON D.ITEM_CODE = ITEM_DIVISION.ITEM_CODE 
     AND D.REVISION = ITEM_DIVISION.REVISION
     AND D.DIVISION_ID = ITEM_DIVISION.DIVISION_ID
   WHERE 1=1
     AND M.DIVISION_ID = {terms["group_id"].V}
     AND M.ORDER_DATE BETWEEN {terms["start_date"].V.Replace("-", "")} AND  {terms["end_date"].V.Replace("-", "")}
     ");



    sSQL.Append($@"
        ORDER BY M.ORDER_DATE   ,    REQUEST_ORDER_SORT_ORDER          
        ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchDetail(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            var sSQL = new StringBuilder();
            sSQL.Append($@"

-- 20250910 납기모듈 납기산출 UI 행 선택 시 모델 사용 자재 정보 표시하는 쿼리 (수정)
-- 납기산출 결과 그리드 (TH_TAR_OM_ORDER_PROMISING)에서 선택한 행의 
-- Item_code, 납기산출요청ID (REQUEST_ORDER_ID), 납기산출요청ID 납기일별 분할 순번ID (REQUEST_ORDER_DUE_SEQ) 를 가지고 필요 자재를 검색하여 표시 
-- 완제픔(item_code) 단위당 자재 필요량, 납기일별 수량에 대한 자재 필요량을 표시. 

-- 기존 쿼리에, 선택한 행의 납기일의 해당 자재 가용량(EOH), 필요 수량 이상의 가용량이 있는 현재 이후 가장 가까운 날짜를 함께 표시함. 
-- 완성 

-- 09.10. 수정
-- Item 자재 필요시점 LT 반영해서 당김. 단, ORDER_DATE 이전까지 가지 않게 보정한 날짜로 변경 

WITH 
today_date as (
	 select {terms["ORDER_DATE"].V} as order_date  -- 당일 날짜의 기준정보 사용 
),
ITEM_MATERIAL_REQ_DATE as (
	select	A.ORDER_DATE, A.DIVISION_ID, A.REQUEST_ORDER_ID, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.TOTAL_SORT_ORDER, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, 
			A.PROMISING_DATE,  --납기일 
			C.ITEM_LT_DAY, -- Item LT 
			greatest(A.order_date, convert(date, dateadd(DAY, isnull(-C.ITEM_LT_DAY, 0), A.PROMISING_DATE) ) ) AS MAT_REQ_DATE,  --##2025.09.10. 자재 필요 시점. 완제품 납기에 LT 반영하고, ORDER_DATE 이전까지 가지 않게 보정한 날짜로 변경 
			QTY_PCS,   -- 완제품 수량임 
			DESCRIPTION
	from	TH_TAR_OM_ORDER_PROMISING A with (nolock)  
			left outer join 
			TH_TAR_OM_ITEM_LEADTIME C  
			on A.DIVISION_ID = C.DIVISION_ID  
			AND A.ITEM_CODE = C.ITEM_CODE 
			and C.USE_YN = 'Y' 
			where A.order_date = (select order_date from today_date) 
			AND A.DIVISION_ID = {terms["DIVISION_ID"].V}			-- UI 선택 
			AND A.REQUEST_ORDER_ID = {terms["REQUEST_ORDER_ID"].D}			-- UI 선택 
			AND A.REQUEST_ORDER_DUE_SEQ = {terms["REQUEST_ORDER_DUE_SEQ"].D} 	-- UI 선택 
)
,
search_date as (	
	 select MAT_REQ_DATE  from ITEM_MATERIAL_REQ_DATE  
),
OM_OP_MATERIAL_LIST AS ( 
	SELECT	--TOP 100 --*  
			A.ORDER_DATE, A.DIVISION_ID, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, A.REQ_MATERIAL_QTY, 
			B.REQUEST_ORDER_ID, B.REQUEST_ORDER_DUE_SEQ, B.REQUEST_ORDER_DUE_SEQ_ORDER, B.TOTAL_SORT_ORDER, 
			B.PROMISING_DATE, 
			B.ITEM_LT_DAY, 
			B.MAT_REQ_DATE, -- LT 반영 날짜 
			B.QTY_PCS, B.DESCRIPTION 
	FROM	TH_TAR_OM_ITEM_REQ_MATERIAL A WITH (NOLOCK)
			INNER JOIN 
			ITEM_MATERIAL_REQ_DATE B 
			ON A.ORDER_DATE = B.ORDER_DATE   -- 오늘 날짜 계획과 필요 자재 목록이 있어야 함 
			AND A.DIVISION_ID = B.DIVISION_ID
			AND A.ITEM_CODE = B.ITEM_CODE
			AND A.REVISION = B.REVISION
			LEFT OUTER JOIN 
			TH_GUI_ITEM_MODEL_SEARCH C   
			ON A.DIVISION_ID = C.DIVISION_ID
			AND A.ITEM_CODE = C.ITEM_CODE 
			AND A.REVISION = FORMAT(CONVERT(INT,C.REV), '000')	
	--WHERE	A.ORDER_DATE =(select order_date from today_date) 
	--AND A.DIVISION_ID = 'SPS'			-- UI 선택 
	--AND B.REQUEST_ORDER_ID = 4			-- UI 선택 
	--AND B.REQUEST_ORDER_DUE_SEQ = 4		-- UI 선택 
	-- AND A.ITEM_CODE = 'MCP23279B00'	-- UI 그리드 선택한 행의 ITEM CODE 
	--ORDER BY A.MATERIAL_ITEM_CODE
) 
,
OM_OP_LINE_MATERIAL_REQ AS (
	SELECT  A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.MAT_REQ_DATE,
			MAX(A.REQ_MATERIAL_QTY) AS REQ_MATERIAL_QTY, 
			SUM(A.QTY_PCS * A.REQ_MATERIAL_QTY) AS REQ_QTY_PCS 
	FROM	OM_OP_MATERIAL_LIST A WITH (NOLOCK)
	GROUP BY A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.MAT_REQ_DATE
	
) 
, 
-- 수량을 PNL로 환산 
OM_OP_LINE_MATERIAL_REQ_PNL as (
	SELECT	A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.MAT_REQ_DATE, A.REQ_MATERIAL_QTY,  
			A.REQ_QTY_PCS, -- 자재 필요 수량 PCS
			-- CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) AS PCS_PER_PNL, 
			CASE WHEN CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) > 0 
				 THEN 1.0 * REQ_QTY_PCS /( CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) ) ELSE NULL END AS REQ_QTY_PNL  -- -- 완제품 요구량에 대한 자재 필요 수량의 PNL 환산 수량
	FROM	OM_OP_LINE_MATERIAL_REQ A 
			LEFT OUTER JOIN 
			TH_GUI_ITEM_MODEL_SEARCH B WITH (NOLOCK)
			ON  A.ITEM_CODE = B.ITEM_CODE
			AND A.REVISION = FORMAT(CONVERT(INT,B.REV), '000')

) 
,
MATERIAL_INOUT_LIST as (
	select	A.ORDER_DATE, A.DIVISION_ID, 
			A.EVENT_DATE, 
			A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, 
			B.MAT_REQ_DATE, 
			B.REQ_MATERIAL_QTY,
			B.REQ_QTY_PCS, B.REQ_QTY_PNL, 
			--A.MATERIAL_QTY, 
			--case when A.INOUT_GBN = 'IN' then isnull(A.MATERIAL_QTY, 0) when A.INOUT_GBN = 'OUT' then  -1*isnull(A.MATERIAL_QTY, 0) else 0 end as MATERIAL_QTY,			
			isnull(A.MATERIAL_QTY, 0) as MATERIAL_QTY,  -- 프로시저에서 음수로 넣고 있음
			A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, 
			rank ()  over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by EVENT_DATE, INOUT_GBN, case when INOUT_CATEGORY = 'INVENTORY' then 1 when INOUT_CATEGORY = 'READY_INPUT' then 2 when INOUT_CATEGORY = 'SAMPLE_SAFETY' then 3 else 9 end, INOUT_CATEGORY,REQUEST_ORDER_SORT_ORDER, REQUEST_ORDER_DUE_SEQ_ORDER)  as LIST_SEQ 
	from	TH_TAR_OM_MATERIAL_INOUT_PLAN A  with (nolock)
			inner join 
			OM_OP_LINE_MATERIAL_REQ_PNL B 
			on 1=1
			--and A.ITEM_CODE = B.ITEM_CODE
			--and A.REVISION = B.REVISION
			and A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE			
	where	A.ORDER_DATE = (select order_date from today_date) --'20250908'
	and		A.EVENT_DATE >= (select order_date from today_date)  -- 현재 일자 이전 데이터는 무시함
) 
,
MATERIAL_INOUT_INOUT_AVAIL_LIST as (	-- order date 이후 전체 날짜에 대한 누적 과부족 계산 
	select A.* 
	from ( 
		select	--top 1 
				--rank() over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by A.LIST_SEQ desc) as rnk, 
				A.ORDER_DATE, A.DIVISION_ID, A.EVENT_DATE, A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, 
				A.REQ_MATERIAL_QTY,
				A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
				A.MATERIAL_QTY, 
				A.REQUEST_ORDER_ID, A.REQUEST_ORDER_DUE_SEQ, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.LIST_SEQ, 
				--max(A.LIST_SEQ) as max_list_seq, 
				--B.search_date, 
				B.MAT_REQ_DATE, 
				sum(A.MATERIAL_QTY) over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by A.LIST_SEQ rows between unbounded preceding and current row) as CUMUL_BALANCE_QTY
		from	MATERIAL_INOUT_LIST A 
				left outer join 
				search_date B
				on 1=1
		where   1=1 --material_item_code = (select material_item_code from material_item_code) --'ACF00090'
		--and     A.EVENT_DATE <= (select search_date from search_date)  -- 여기서는 전체 기간 과부족 산출 
		--order by A.LIST_SEQ desc  
	) A
	
) 
,
MATERIAL_INOUT_PREV_LIST as (	
	select	A.ORDER_DATE, 
			A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
			A.EVENT_DATE, A.MAT_REQ_DATE, A.AVAILABLE_QTY, A.rnk
	from ( 
		select	
				A.ORDER_DATE, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
				A.EVENT_DATE, A.MAT_REQ_DATE, 
				A.CUMUL_BALANCE_QTY as AVAILABLE_QTY, 
				rank() over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by A.LIST_SEQ desc) as rnk 
		from	MATERIAL_INOUT_INOUT_AVAIL_LIST A
		where   A.EVENT_DATE <= (select MAT_REQ_DATE from search_date)  -- search_date 이전의 마지막 가용량이 search_date의 가용량 
		
	) A
	where A.rnk = 1 
)
,
MATERIAL_AVAILABLE_QTY as (	
	select	A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
			--B.SEARCH_DATE, 
			B.MAT_REQ_DATE,
			--B.EVENT_DATE as last_event_date, 
			B.AVAILABLE_QTY -- 납기일 기준의 자재 가용량
	from	OM_OP_LINE_MATERIAL_REQ_PNL A  
			left outer join 
			MATERIAL_INOUT_PREV_LIST B 
			on A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE
) 
,
-- 3) 가용량이 필요량 이상인 MAT_REQ_DATE 이후의 가장 가까운 일자를 구하기 위해, 전체 누적 가용량을 사용 
MATERIAL_INOUT_FUTURE_LIST as (	-- 조회 기준일과 그 이전 데이터만 조회하고, max seq 확인 
	select * 
	from (	
		select	A.*, 
				rank() over (partition by A.MATERIAL_ITEM_CODE order by A.LIST_SEQ) as rnk 
		from ( 
			-- 납기일 기준 가용량이 충분하면, 이게 공급 가능일. 
			select A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
				   A.MAT_REQ_DATE as EVENT_DATE, --B.EVENT_DATE as last_event_date, 
  				   A.AVAILABLE_QTY, 0 as LIST_SEQ  -- search date 가용량이 필요량 보다 많거나 같으면 search date에 가용으로 표시 (LIST_SEQ 최우선순위) 
			from	MATERIAL_AVAILABLE_QTY A 
			where  A.REQ_QTY_PNL <= A.AVAILABLE_QTY   -- search date 가용량이 필요량 보다 많거나 같으면 search date에 가용으로 표시 
			-- 납기일 이후의 가용량 중에서 필요량 이상인 것 
			union all 
			select	
					A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
					B.EVENT_DATE, -- 가용량이 필요량 이상인 SEARCH_DATE 이후의 가장 가까운 일자  
  					B.CUMUL_BALANCE_QTY as AVAILABLE_QTY, B.LIST_SEQ    	
			from	MATERIAL_AVAILABLE_QTY A  
					inner join --left outer join 
					MATERIAL_INOUT_INOUT_AVAIL_LIST B 
					on a.MATERIAL_ITEM_CODE = b.MATERIAL_ITEM_CODE  
			--where   1=1 --material_item_code = (select material_item_code from material_item_code) --'ACF00090'
			and     B.EVENT_DATE > A.MAT_REQ_DATE -- (select search_date from search_date)
			and     A.REQ_QTY_PNL <= B.CUMUL_BALANCE_QTY  -- 가용량이 필요량 이상
			----order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ desc  -- 반드시 필요 
		) A
	) A 
	where A.rnk = 1 
	--order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ 
)  
select	A.ORDER_DATE, -- 납기약속 계획일자 
		--A.ITEM_CODE, A.REVISION, 
		A.MATERIAL_ITEM_CODE, 
		A.REQ_MATERIAL_QTY,  
		A.MAT_REQ_DATE as REQ_DATE, 
        (select dbo.GET_OM_ITEM_YIELD ({terms["ITEM_CODE"].V})) AS YIELD,
        ROUND(A.REQ_QTY_PCS / (select dbo.GET_OM_ITEM_YIELD ({terms["ITEM_CODE"].V})), 2) AS REQ_QTY_PCS_YIELD,
        CEILING( (select dbo.GET_CONVERSION_QTY({terms["ITEM_CODE"].V},  (A.REQ_QTY_PCS / (select dbo.GET_OM_ITEM_YIELD ({terms["ITEM_CODE"].V}))), 'PNL')) ) AS REQ_QTY_PNL_YIELD,
		A.REQ_QTY_PCS, A.REQ_QTY_PNL, 		
		A.EVENT_DATE, -- 가용량 정보가 존재하는 SEARCH_DATE 또는 그 이전의 가장 최근 날짜 
		A.AVAILABLE_QTY as AVAILABLE_QTY_ON_SEARCH_DATE, -- search_date 시점의 가용량 	
		B.EVENT_DATE as FASTEST_SUPPLIABLE_DATE,  B.AVAILABLE_QTY as SUPPLIABLE_QTY 
from	MATERIAL_INOUT_PREV_LIST A 
		left outer join 
		MATERIAL_INOUT_FUTURE_LIST B
		on A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE 
;
    ");

            sSQL.Append($@"
              -- ORDER BY D.REQUEST_ORDER_ID ,  D.REQUEST_ORDER_DUE_SEQ          
              ");

            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchDetail22222222222222222222222222222222222222222222222222(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            var sSQL = new StringBuilder();
            sSQL.Append($@"


with 
today_date as (
	 -- select   format(getdate(),'yyyyMMdd') as order_date  -- 당일 날짜의 기준정보 사용 
    select {terms["ORDER_DATE"].V}  as order_date
), 
ITEM_MAX_REV AS (
	          SELECT  DIVISION_ID
                  , ITEM_CODE
                  , FORMAT(CONVERT(INT,MAX(REV)), '000') AS REVISION	--## 2025.09.08. 수정. 1, 001 두 가지 형태 혼재. 
	          FROM  TH_GUI_ITEM_MODEL_SEARCH B WITH(NOLOCK)
	          GROUP BY  DIVISION_ID
                    , ITEM_CODE
),
ORDER_PROMISING as (
	select A.ORDER_DATE, A.DIVISION_ID, A.REQUEST_ORDER_ID, B.REQUEST_ORDER_SORT_ORDER,  A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.TOTAL_SORT_ORDER, A.ITEM_CODE, A.MODEL_NAME, C.REVISION, A.PROMISING_DATE, A.QTY_PCS
	from TH_TAR_OM_ORDER_PROMISING A with (nolock)  -- select * from  TH_TAR_OM_ORDER_PROMISING with (nolock)
		 inner join 
		 TH_TAR_OM_ORDER_REQUEST B with (nolock)  -- select * from  TH_TAR_OM_ORDER_REQUEST with (nolock)
		 on A.ORDER_DATE = B.ORDER_DATE
		 and A.DIVISION_ID = B.DIVISION_ID
		 and A.REQUEST_ORDER_ID = B.REQUEST_ORDER_ID
		 left outer join 
		 ITEM_MAX_REV C 
		 on  A.ITEM_CODE = C.ITEM_CODE
	where A.order_date  =  ({terms["ORDER_DATE"].V} )   -- ########################
)
--select * from  ORDER_PROMISING 
, 
ORDER_PROMISING_MAT_REQ as ( 
	select  A.ORDER_DATE, A.DIVISION_ID, A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, 
			A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.PROMISING_DATE, 
			B.MATERIAL_ITEM_CODE, B.MATERIAL_ITEM_NAME,
			A.QTY_PCS as order_qty,
			B.REQ_MATERIAL_QTY as CONSUME_RATE, 
			--####### 아래 2줄 수정 
			A.QTY_PCS * isnull(B.REQ_MATERIAL_QTY, 1) as MATERIAL_QTY_PCS, 
			--###########  2025.09.03. 아래 수식 괄호 추가 (3개 항 나누기) 
			case when convert(float, C.UP)*  convert(float, ARRANGEMENT_X) * convert(float, ARRANGEMENT_Y) > 0 
			     then ceiling( 1.0*A.QTY_PCS / ( convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) ) )  else null end as MATERIAL_QTY --  MATERIAL_QTY_PNL
	from ORDER_PROMISING A 
		 inner join 
		 TH_TAR_OM_ITEM_REQ_MATERIAL B with (nolock)  -- select *  from  TH_TAR_OM_ITEM_REQ_MATERIAL with (nolock) where order_date  =  (select format(getdate(),'yyyyMMdd') as order_date ) and item_code in ('MCP23685A00') --, 'MCP23279B00' ) 
		 on A.DIVISION_ID = B.DIVISION_ID
		 and A.item_code = B.item_code 
		 and A.revision = B.revision 
		 --############ 아래 추가 
		 left outer join 
		 TH_GUI_ITEM_MODEL_SEARCH C   -- SQM 환산 정보.  select top 100 * from TH_GUI_ITEM_MODEL_SEARCH B with (nolock) 	
		 on A.DIVISION_ID = C.DIVISION_ID
		 and A.item_code = C.item_code 
		 and B.revision = FORMAT(CONVERT(INT,C.REV), '000')		--## 2025.09.08. 수정. -- C.REV  
		 where B.ORDER_DATE =  ({terms["ORDER_DATE"].V} )  -- ########################
)
--select * from  ORDER_PROMISING_MAT_REQ 
--insert into TH_TAR_OM_MATERIAL_INOUT_PLAN
--     (A.ORDER_DATE, A.DIVISION_ID, A.EVENT_DATE, A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, A.MATERIAL_QTY, A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.INSERT_ID, A.INSERT_DTTM, A.UPDATE_ID, A.UPDATE_DTTM)
select	B.order_date, A.DIVISION_ID, 
		--B.order_date AS EVENT_DATE, 
		--A.PROMISING_DATE AS EVENT_DATE,  --## 2025.09.08. 수정  
		--> 2025.09.10. 수정  LT 반영하고, ORDER_DATE 이전까지 가지 않게 보정한 날짜로 변경 
		A.PROMISING_DATE, 
		--############  2025-09-10 수정   LT만큼 자재 필요시점을 당기고, ORDER_DATE보다 빠르지 않게 
		greatest(B.order_date, convert(date, dateadd(DAY, isnull(-C.ITEM_LT_DAY, 0), A.PROMISING_DATE) ) ) AS EVENT_DATE,  --##2025.09.10. 수정  LT 반영하고, ORDER_DATE 이전까지 가지 않게 보정한 날짜로 변경 
		'OUT' INOUT_GBN, 'OUTGOING' INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME,
		-1.0*A.MATERIAL_QTY as MATERIAL_QTY, -- PNL 수량임 
		A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER,
		A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, 
		A.ITEM_CODE, A.MODEL_NAME, A.REVISION,
		'ADMIN' INSERT_ID, getdate() INSERT_DTTM, 'ADMIN' UPDATE_ID, getdate() UPDATE_DTTM
from	ORDER_PROMISING_MAT_REQ A  with (nolock)
		join 
		today_date B 
		on 1=1
		--############  2025-09-10 수정
		left outer join 
		TH_TAR_OM_ITEM_LEADTIME C  -- OM Lead Time -- select * from  TH_TAR_OM_ITEM_LEADTIME with (nolock) where USE_YN = 'Y' and item_code = 'BOC00994A00' 
		on A.DIVISION_ID = C.DIVISION_ID  
		AND A.ITEM_CODE = C.ITEM_CODE 
		and C.USE_YN = 'Y' 
;















WITH 
today_date as (
	-- select format(getdate(),'yyyyMMdd') as order_date  -- 당일 날짜의 기준정보 사용 
	select {terms["ORDER_DATE"].V}  as order_date
),
search_date as (
	 select {terms["PROMISING_DATE"].V}  as search_date -- TH_TAR_OM_ORDER_PROMISING 에서 선택한 행의 PROMISING_DATE 
),
OM_OP_MATERIAL_LIST AS ( 
	SELECT	--TOP 100 --*  
			A.ORDER_DATE, A.DIVISION_ID, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, A.REQ_MATERIAL_QTY, 
			B.REQUEST_ORDER_ID, B.REQUEST_ORDER_DUE_SEQ, B.REQUEST_ORDER_DUE_SEQ_ORDER, B.TOTAL_SORT_ORDER, B.PROMISING_DATE, B.QTY_PCS, B.DESCRIPTION 
	FROM	TH_TAR_OM_ITEM_REQ_MATERIAL A WITH (NOLOCK)
			INNER JOIN 
			TH_TAR_OM_ORDER_PROMISING B WITH (NOLOCK) 
			ON A.ORDER_DATE = B.ORDER_DATE   -- 오늘 날짜 계획과 필요 자재 목록이 있어야 함 
			AND A.DIVISION_ID = B.DIVISION_ID
			AND A.ITEM_CODE = B.ITEM_CODE
			AND A.REVISION = B.REVISION
			LEFT OUTER JOIN 
			TH_GUI_ITEM_MODEL_SEARCH C   -- SQM 환산 정보.  SELECT TOP 100 * FROM TH_GUI_ITEM_MODEL_SEARCH B WITH (NOLOCK) 	
			ON A.DIVISION_ID = C.DIVISION_ID
			AND A.ITEM_CODE = C.ITEM_CODE 
			AND A.REVISION = C.REV
	WHERE	A.ORDER_DATE =(select order_date from today_date) --  (select format(getdate(),'yyyyMMdd' ))  -- 당일 날짜의 기준정보 사용  -- '20250904' 
	AND A.DIVISION_ID = {terms["DIVISION_ID"].V}			-- UI 선택 
	AND B.REQUEST_ORDER_ID = {terms["REQUEST_ORDER_ID"].D}			-- UI 선택 
	AND B.REQUEST_ORDER_DUE_SEQ = {terms["REQUEST_ORDER_DUE_SEQ"].D}			-- UI 선택 
	-- AND A.ITEM_CODE = {terms["ITEM_CODE"].V}	-- UI 그리드 선택한 행의 ITEM CODE 
	--ORDER BY A.MATERIAL_ITEM_CODE
)--select * from OM_OP_MATERIAL_LIST
-- 선택한 LINE의 자재 
,
OM_OP_LINE_MATERIAL_REQ AS (
	SELECT  A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, MAX(A.REQ_MATERIAL_QTY) AS REQ_MATERIAL_QTY, SUM(A.QTY_PCS * A.REQ_MATERIAL_QTY) AS REQ_QTY_PCS 
	FROM	OM_OP_MATERIAL_LIST A WITH (NOLOCK)
	GROUP BY A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE
	--ORDER BY A.MATERIAL_ITEM_CODE
)  , 
OM_OP_LINE_MATERIAL_REQ_PNL as (
	SELECT	A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  A.REQ_QTY_PCS, 
			-- CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) AS PCS_PER_PNL, 
			CASE WHEN CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) > 0 
				 THEN 1.0 * REQ_QTY_PCS /( CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) ) ELSE NULL END AS REQ_QTY_PNL  
	FROM	OM_OP_LINE_MATERIAL_REQ A 
			LEFT OUTER JOIN 
			TH_GUI_ITEM_MODEL_SEARCH B WITH (NOLOCK)
			ON  A.ITEM_CODE = B.ITEM_CODE
			AND A.REVISION = FORMAT(CONVERT(INT,B.REV), '000')

) --select * from  OM_OP_LINE_MATERIAL_REQ_PNL with (nolock)
,
MATERIAL_INOUT_LIST as (
	select	A.ORDER_DATE, A.DIVISION_ID, A.EVENT_DATE, A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, 
			B.REQ_MATERIAL_QTY,
			--A.MATERIAL_QTY, 
			--case when A.INOUT_GBN = 'IN' then isnull(A.MATERIAL_QTY, 0) when A.INOUT_GBN = 'OUT' then  -1*isnull(A.MATERIAL_QTY, 0) else 0 end as MATERIAL_QTY,			
			isnull(A.MATERIAL_QTY, 0) as MATERIAL_QTY,  -- 프로시저에서 음수로 넣고 있음
			A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, 
			rank ()  over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by EVENT_DATE, INOUT_GBN, case when INOUT_CATEGORY = 'INVENTORY' then 1 else 0 end, INOUT_CATEGORY,REQUEST_ORDER_SORT_ORDER, REQUEST_ORDER_DUE_SEQ_ORDER)  as LIST_SEQ 
	from	TH_TAR_OM_MATERIAL_INOUT_PLAN A  with (nolock)
			inner join 
			OM_OP_LINE_MATERIAL_REQ_PNL B 
			on 1=1
			--and A.ITEM_CODE = B.ITEM_CODE
			--and A.REVISION = B.REVISION
			and A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE
	where	A.ORDER_DATE = (select order_date from today_date) --'20250908'
	and		A.EVENT_DATE >= convert(date, getdate())  -- 현재 일자 이전 데이터는 무시함
)
,

MATERIAL_INOUT_INOUT_AVAIL_LIST as (	-- order date 이후 전체 날짜에 대한 누적 과부족 계산 
	select A.* 
	from ( 
		select	--top 1 
				--rank() over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by A.LIST_SEQ desc) as rnk, 
				A.ORDER_DATE, A.DIVISION_ID, A.EVENT_DATE, A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, 
				A.REQ_MATERIAL_QTY,
				A.MATERIAL_QTY, 
				A.REQUEST_ORDER_ID, A.REQUEST_ORDER_DUE_SEQ, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.LIST_SEQ, 
				--max(A.LIST_SEQ) as max_list_seq, 
				B.search_date, 
				sum(A.MATERIAL_QTY) over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by A.LIST_SEQ rows between unbounded preceding and current row) as CUMUL_BALANCE_QTY
		from	MATERIAL_INOUT_LIST A 
				left outer join 
				search_date B
				on 1=1
		where   1=1 --material_item_code = (select material_item_code from material_item_code) --'ACF00090'
		--and     A.EVENT_DATE <= (select search_date from search_date)  -- 여기서는 전체 기간 과부족 산출 
		--order by A.LIST_SEQ desc  
	) A
	--where A.rnk = 1 
) -- select * from  MATERIAL_INOUT_INOUT_AVAIL_LIST  with (nolock) order by MATERIAL_ITEM_CODE, LIST_SEQ
,
-- 2) SEARCH_DATE의 가용량
MATERIAL_INOUT_PREV_LIST as (	-- SEARCH_DATE의 가용량을 알기 위해 조회 기준일과 그 이전 데이터만 조회하고, max seq 확인 
	select	A.ORDER_DATE, --A.ITEM_CODE, A.REVISION, 
			A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  --A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
			A.EVENT_DATE, A.SEARCH_DATE, A.AVAILABLE_QTY, A.rnk
	from ( 
		select	--A.ITEM_CODE, A.REVISION, 
				A.ORDER_DATE, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  --A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
				A.EVENT_DATE, A.SEARCH_DATE, 
				A.CUMUL_BALANCE_QTY as AVAILABLE_QTY, 
				rank() over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by A.LIST_SEQ desc) as rnk 
		from	MATERIAL_INOUT_INOUT_AVAIL_LIST A
		where   A.EVENT_DATE <= (select search_date from search_date)  -- search_date 이전의 마지막 가용량이 search_date의 가용량 
		--order by A.LIST_SEQ desc  
	) A
	where A.rnk = 1 
)
--select * from MATERIAL_INOUT_PREV_LIST; 
-- 완제품 납기 search_date 기준으로 자재 가용량 
,
MATERIAL_AVAILABLE_QTY as (
	--select * from   MATERIAL_INOUT_PREV_LIST
	--select MATERIAL_ITEM_CODE, search_date, EVENT_DATE as LAST_MATERIAL_EVENT_DATE, CUMUL_BALANCE_QTY, rnk from  MATERIAL_INOUT_PREV_LIST with (nolock) order by material_item_code, rnk
	select	A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
			B.SEARCH_DATE, --B.EVENT_DATE as last_event_date, 
			AVAILABLE_QTY -- 납기일 기준의 자재 가용량
	from	OM_OP_LINE_MATERIAL_REQ_PNL A  
			left outer join 
			MATERIAL_INOUT_PREV_LIST B 
			on A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE
) --select * from MATERIAL_AVAILABLE_QTY; 
,
-- 3) 가용량이 필요량 이상인 SEARCH_DATE 이후의 가장 가까운 일자를 구하기 위해, 전체 누적 가용량을 사용 
MATERIAL_INOUT_FUTURE_LIST as (	-- 조회 기준일과 그 이전 데이터만 조회하고, max seq 확인 
	select * 
	from (	
		select	A.*, 
				rank() over (partition by A.MATERIAL_ITEM_CODE order by A.LIST_SEQ) as rnk 
		from ( 
			-- 납기일 기준 가용량이 충분하면, 이게 공급 가능일. 
			select A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
				   A.SEARCH_DATE as EVENT_DATE, --B.EVENT_DATE as last_event_date, 
  				   A.AVAILABLE_QTY, 0 as LIST_SEQ  -- search date 가용량이 필요량 보다 많거나 같으면 search date에 가용으로 표시 (LIST_SEQ 최우선순위) 
			from	MATERIAL_AVAILABLE_QTY A 
			where  A.REQ_QTY_PNL <= A.AVAILABLE_QTY   -- search date 가용량이 필요량 보다 많거나 같으면 search date에 가용으로 표시 
			-- 납기일 이후의 가용량 중에서 필요량 이상인 것 
			union all 
			select	--A.* 
					A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
					B.EVENT_DATE, -- 가용량이 필요량 이상인 SEARCH_DATE 이후의 가장 가까운 일자  
  					B.CUMUL_BALANCE_QTY as AVAILABLE_QTY, B.LIST_SEQ    	
			from	MATERIAL_AVAILABLE_QTY A  
					inner join --left outer join 
					MATERIAL_INOUT_INOUT_AVAIL_LIST B 
					on a.MATERIAL_ITEM_CODE = b.MATERIAL_ITEM_CODE  
			--where   1=1 --material_item_code = (select material_item_code from material_item_code) --'ACF00090'
			and     B.EVENT_DATE > A.search_date -- (select search_date from search_date)
			and     A.REQ_QTY_PNL <= B.CUMUL_BALANCE_QTY  -- 가용량이 필요량 이상
			----order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ desc  -- 반드시 필요 
		) A
	) A 
	where A.rnk = 1 
	--order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ 
)  
select	A.ORDER_DATE, -- 납기약속 계획일자 
		--A.ITEM_CODE, A.REVISION, 
		A.MATERIAL_ITEM_CODE, 
		A.REQ_MATERIAL_QTY,  
		A.SEARCH_DATE as REQ_DATE, 
		B.REQ_QTY_PCS, B.REQ_QTY_PNL, 		
		A.EVENT_DATE, -- 가용량 정보가 존재하는 SEARCH_DATE 또는 그 이전의 가장 최근 날짜 
		A.AVAILABLE_QTY as AVAILABLE_QTY_ON_SEARCH_DATE, -- search_date 시점의 가용량 	
		B.EVENT_DATE as FASTEST_SUPPLIABLE_DATE,  B.AVAILABLE_QTY as SUPPLIABLE_QTY 
from	MATERIAL_INOUT_PREV_LIST A 
		left outer join 
		MATERIAL_INOUT_FUTURE_LIST B
		on A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE 
;
    ");
          
            sSQL.Append($@"
              -- ORDER BY D.REQUEST_ORDER_ID ,  D.REQUEST_ORDER_DUE_SEQ          
              ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }



        private void SavePROMISING(ParamList detailList)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"

                DECLARE @ORDER_DATE nvarchar(16) ;
                DECLARE @DIVISION_ID nvarchar(20) ;
                DECLARE @REQUEST_ORDER_ID int ;
                DECLARE @REQUEST_ORDER_DUE_SEQ int ;
                DECLARE @REQUEST_ORDER_DUE_SEQ_ORDER int ;
                DECLARE @TOTAL_SORT_ORDER int ;
                DECLARE @ITEM_CODE nvarchar(510) ;
                DECLARE @MODEL_NAME nvarchar(2000);
                DECLARE @REVISION nvarchar(6) ;
                DECLARE @PROMISING_DATE nvarchar(20) ;
                DECLARE @QTY_PCS float ;
                DECLARE @DESCRIPTION nvarchar(2000) ;
                DECLARE @INSERT_ID nvarchar(100) ;
                DECLARE @INSERT_DTTM datetime ;
                DECLARE @UPDATE_ID nvarchar(100) ;
                DECLARE @UPDATE_DTTM datetime ;

            ");
            foreach (Params ITEM in detailList)
            {
                DTClient.UserInfoMerge(ITEM);
                sSQL.Append($@"

 SET @ORDER_DATE  = {ITEM["ORDER_DATE"].V};
 SET @DIVISION_ID  = {ITEM["DIVISION_ID"].V};
 SET @REQUEST_ORDER_ID  = {ITEM["REQUEST_ORDER_ID"].V};
 SET @REQUEST_ORDER_DUE_SEQ = {ITEM["REQUEST_ORDER_DUE_SEQ"].V};
 SET @REQUEST_ORDER_DUE_SEQ_ORDER  = {ITEM["REQUEST_ORDER_DUE_SEQ_ORDER"].V};
 SET @TOTAL_SORT_ORDER  = {ITEM["TOTAL_SORT_ORDER"].V};
 SET @ITEM_CODE = {ITEM["ITEM_CODE"].V};
 SET @MODEL_NAME  = {ITEM["MODEL_NAME"].V};
 SET @REVISION  = {ITEM["REVISION"].V};
 SET @PROMISING_DATE  = {ITEM["PROMISING_DATE"].V};
 SET @QTY_PCS  = {ITEM["QTY_PCS"].V};
 SET @DESCRIPTION  = {ITEM["DESCRIPTION"].V};
 SET @INSERT_ID  = {ITEM["USER_ID"].V};
 SET @INSERT_DTTM = {ITEM["INSERT_DTTM"].V};
 SET @UPDATE_ID  = {ITEM["USER_ID"].V};
 SET @UPDATE_DTTM = {ITEM["UPDATE_DTTM"].V};


 
IF @PROMISING_DATE IS NOT NULL
BEGIN
    SET @PROMISING_DATE = CONVERT(VARCHAR(10), @PROMISING_DATE, 120) ;
END


IF EXISTS (
    SELECT 1 FROM TH_TAR_OM_ORDER_PROMISING 
    WHERE 
          DIVISION_ID = @DIVISION_ID
      AND ORDER_DATE = @ORDER_DATE
      AND REQUEST_ORDER_ID = @REQUEST_ORDER_ID 
      AND REQUEST_ORDER_DUE_SEQ = @REQUEST_ORDER_DUE_SEQ
)
BEGIN
    UPDATE TH_TAR_OM_ORDER_PROMISING SET   
        TOTAL_SORT_ORDER = @TOTAL_SORT_ORDER,
        ITEM_CODE = @ITEM_CODE,
        MODEL_NAME = @MODEL_NAME,
        REVISION = @REVISION,
        PROMISING_DATE = @PROMISING_DATE,
        QTY_PCS = @QTY_PCS,
        DESCRIPTION = @DESCRIPTION,
        UPDATE_ID = @UPDATE_ID,
        UPDATE_DTTM = @UPDATE_DTTM
     WHERE 
          DIVISION_ID = @DIVISION_ID
      AND ORDER_DATE = @ORDER_DATE
      AND REQUEST_ORDER_ID = @REQUEST_ORDER_ID 
      AND REQUEST_ORDER_DUE_SEQ = @REQUEST_ORDER_DUE_SEQ
END
ELSE
BEGIN


    -- 자동 증가 로직
    SELECT @REQUEST_ORDER_DUE_SEQ = 
        ISNULL(MAX(REQUEST_ORDER_DUE_SEQ), 0) + 1
    FROM TH_TAR_OM_ORDER_PROMISING
    WHERE ORDER_DATE = @ORDER_DATE
      AND DIVISION_ID = @DIVISION_ID
      AND REQUEST_ORDER_ID = @REQUEST_ORDER_ID;


    INSERT INTO TH_TAR_OM_ORDER_PROMISING (
        ORDER_DATE,
        DIVISION_ID,
        REQUEST_ORDER_ID,
        REQUEST_ORDER_DUE_SEQ,
        REQUEST_ORDER_DUE_SEQ_ORDER,
        TOTAL_SORT_ORDER,
        ITEM_CODE,
        MODEL_NAME,
        REVISION,
        PROMISING_DATE,
        QTY_PCS,
        DESCRIPTION,
        INSERT_ID,
        INSERT_DTTM,
        UPDATE_ID,
        UPDATE_DTTM
    )
    VALUES (
        @ORDER_DATE,
        @DIVISION_ID,
        @REQUEST_ORDER_ID,
        @REQUEST_ORDER_DUE_SEQ,
        @REQUEST_ORDER_DUE_SEQ_ORDER,
        @TOTAL_SORT_ORDER,
        @ITEM_CODE,
        @MODEL_NAME,
        @REVISION,
        @PROMISING_DATE,
        @QTY_PCS,
        @DESCRIPTION,
        @INSERT_ID,
        @INSERT_DTTM,
        @UPDATE_ID,
        @UPDATE_DTTM
    );
END;

");
            }

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }


        private void deletePROMISING(ParamList data)
        {
            StringBuilder sSQL = new StringBuilder();
            data.ForEach(ITEM =>
            {
                sSQL.Append($@"
                  DELETE FROM TH_TAR_OM_ORDER_PROMISING        
                   WHERE ORDER_DATE  = {ITEM["ORDER_DATE"].V} 
                     AND DIVISION_ID = {ITEM["DIVISION_ID"].V} 
                     AND REQUEST_ORDER_ID = {ITEM["REQUEST_ORDER_ID"].V} 
                     AND REQUEST_ORDER_DUE_SEQ = {ITEM["REQUEST_ORDER_DUE_SEQ"].V}                                    
                ;");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        private DataTable GET_CBST_SPEC_BASIC(Params terms)
        {
            var sSQL = new StringBuilder();
            sSQL.Append($@"

                -- MAX 리비전만 표시 
                WITH RankedItems AS (
                    SELECT 
                        ITEM_CODE,
                        REVISION,
                        MODEL_REV,
                        CUSTOMER,
                        D_CATEGORY,
                        MODEL_NAME,
                        ROW_NUMBER() OVER (PARTITION BY ITEM_CODE ORDER BY REVISION DESC) AS rn
                    FROM CBST_SPEC_BASIC
                   WHERE ITEM_CODE= {terms["ITEM_CODE"].V}
                )
                SELECT 
                    A.ITEM_CODE,
                    A.REVISION,
                    A.MODEL_REV,
                    A.CUSTOMER,
                    A.D_CATEGORY,
                    A.MODEL_NAME,
                    C.CUSTOMER_NAME AS CUSTOMER_NAME,
                    A.CUSTOMER AS CUST_ID,
                    C.CUSTOMER_NAME AS CUST_NAME
                FROM RankedItems A 
                LEFT OUTER JOIN AR_CUSTOMERS C ON A.CUSTOMER =  C.CUSTOMER_NUMBER
                WHERE A.rn = 1
;               
              ");            
            return Data.Get(sSQL.ToString()).Tables[0];
        }




    }
}

