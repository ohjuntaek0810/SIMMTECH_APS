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
                vali.Null("MODEL_NAME", "Item ������ �ʼ� �Է� �����Դϴ�.");
                vali.Null("REVISION", "Item ������ �ʼ� �Է� �����Դϴ�.");
                vali.Null("ORDER_DATE", "ORDER_DATE �� �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("DIVISION_ID", "DIVISION_ID �� �Էµ��� �ʾҽ��ϴ�.");
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
                vali.Null("PLAN_ID", "PLAN_ID �� �����ϴ�.");              
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
                return true; // ����
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.ToString());
                return false; // ����
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
                // �� �࿡ ���� �۾� ����
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
                    M.CUST_ID,		-- UI ��ǥ��. DB ����.  
                    M.CUST_NAME,
                    M.SHIP_TO_ID,	-- UI ��ǥ��. DB ����.  
                    M.SHIP_TO_NAME,
                    M.DIVISION_ID,	-- UI ��ǥ��. ���� ����.  
                    M.MODEL_NAME,
                    M.PO_NUMBER,
                    M.CUST_PO_NUMBER,  -- UI ��ǥ��.
			        M.REQUEST_ORDER_ID,			-- �����û ����
                    M.REQUEST_ORDER_SORT_ORDER, -- UI ��ǥ��. �����û �� ǥ�� ����1
                    D.REQUEST_ORDER_DUE_SEQ,	-- ������ ���� (���ڼ� �ƴ�, ����ڰ� ���� ����) 
                    D.REQUEST_ORDER_DUE_SEQ_ORDER, -- UI ��ǥ��. �����û�� ������ �� ǥ�� ����2
                    D.TOTAL_SORT_ORDER, -- UI ��ǥ��
                    M.ITEM_CODE,
                    M.REVISION,		    -- UI ��ǥ��.
                    M.TOTAL_QTY_PCS,	-- ��û �� ���� (PCS)
			        D.QTY_PCS,			-- �����Ϻ� ���� (PCS) 
			        case when convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) > 0 
				            then round(1.0*M.TOTAL_QTY_PCS/( convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) ) * convert(float, C.PANEL_SIZE_X) * convert(float, C.PANEL_SIZE_Y)/1000000, 2)  
				            else null end 
			        as TOTAL_QTY_SQM,	-- ��û �� ���� (SQM)
			        case when convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) > 0 
				            then round(1.0*D.QTY_PCS/( convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) ) * convert(float, C.PANEL_SIZE_X) * convert(float, C.PANEL_SIZE_Y)/1000000, 2)  
				            else null end 
			        as QTY_SQM,			-- �����Ϻ� ���� (SQM)
			        D.PROMISING_DATE,	-- ������
			        -- M.PROMISE_DATE,  -- �̻��
                    LT.ITEM_LT_DAY as LeadTime, -- Lead Time 
			        (1.0-isnull(convert(float, IPG.SHRINKAGE_RATE), 0.0)) as yield, -- ����
                    M.DESCRIPTION AS M_DESCRIPTION, -- UI ��ǥ��.
                    M.REQUEST_DATE,			-- ��û��
                    D.DESCRIPTION  AS D_DESCRIPTION,   -- Remark, �Է� ���� ����
                    FLOOR(JC.TOT_JIG_CAPA_PCS_with_YIELD) AS BBT_CAPA,
			        M.SCHEDULE_SHIP_DATE,	-- UI ��ǥ��.
                    M.SHIP_DATE,			-- UI ��ǥ��.            
                   ");
                    foreach (DataRow row in dt.Rows)
                    {
                        // �� �࿡ ���� �۾� ����
                     
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
	            AND M.REVISION = FORMAT(CONVERT(INT,C.REV), '000')  -- Rev �� ������� �� ���� 
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
                    D.order_date = {terms["search_order_date"].V.Replace("-", "")} --  UI ��ȸ���� ���� Order datea 
	            order by M.ORDER_DATE, M.REQUEST_ORDER_SORT_ORDER, -- UI ��ǥ��. �����û �� ǥ�� ����1
                        D.REQUEST_ORDER_DUE_SEQ_ORDER -- UI ��ǥ��. �����û�� ������ �� ǥ�� ����2

        ");


            //Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchMaster���250911(Params terms)
        {
            DTClient.UserInfoMerge(terms);


            DataTable dt = DT_OM_BOTTLE_NECK_LIST_V(); ;


            foreach (DataRow row in dt.Rows)
            {
                // �� �࿡ ���� �۾� ����
                Console.WriteLine(row["BOTTLE_NECK_NAME"]); // ��: row["Name"]
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
                // �� �࿡ ���� �۾� ����
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
                // �� �࿡ ���� �۾� ����
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
                // �� �࿡ ���� �۾� ����
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

-- 20250910 ������ ������� UI �� ���� �� �� ��� ���� ���� ǥ���ϴ� ���� (����)
-- ������� ��� �׸��� (TH_TAR_OM_ORDER_PROMISING)���� ������ ���� 
-- Item_code, ��������ûID (REQUEST_ORDER_ID), ��������ûID �����Ϻ� ���� ����ID (REQUEST_ORDER_DUE_SEQ) �� ������ �ʿ� ���縦 �˻��Ͽ� ǥ�� 
-- ������(item_code) ������ ���� �ʿ䷮, �����Ϻ� ������ ���� ���� �ʿ䷮�� ǥ��. 

-- ���� ������, ������ ���� �������� �ش� ���� ���뷮(EOH), �ʿ� ���� �̻��� ���뷮�� �ִ� ���� ���� ���� ����� ��¥�� �Բ� ǥ����. 
-- �ϼ� 

-- 09.10. ����
-- Item ���� �ʿ���� LT �ݿ��ؼ� ���. ��, ORDER_DATE �������� ���� �ʰ� ������ ��¥�� ���� 

WITH 
today_date as (
	 select {terms["ORDER_DATE"].V} as order_date  -- ���� ��¥�� �������� ��� 
),
ITEM_MATERIAL_REQ_DATE as (
	select	A.ORDER_DATE, A.DIVISION_ID, A.REQUEST_ORDER_ID, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.TOTAL_SORT_ORDER, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, 
			A.PROMISING_DATE,  --������ 
			C.ITEM_LT_DAY, -- Item LT 
			greatest(A.order_date, convert(date, dateadd(DAY, isnull(-C.ITEM_LT_DAY, 0), A.PROMISING_DATE) ) ) AS MAT_REQ_DATE,  --##2025.09.10. ���� �ʿ� ����. ����ǰ ���⿡ LT �ݿ��ϰ�, ORDER_DATE �������� ���� �ʰ� ������ ��¥�� ���� 
			QTY_PCS,   -- ����ǰ ������ 
			DESCRIPTION
	from	TH_TAR_OM_ORDER_PROMISING A with (nolock)  
			left outer join 
			TH_TAR_OM_ITEM_LEADTIME C  
			on A.DIVISION_ID = C.DIVISION_ID  
			AND A.ITEM_CODE = C.ITEM_CODE 
			and C.USE_YN = 'Y' 
			where A.order_date = (select order_date from today_date) 
			AND A.DIVISION_ID = {terms["DIVISION_ID"].V}			-- UI ���� 
			AND A.REQUEST_ORDER_ID = {terms["REQUEST_ORDER_ID"].D}			-- UI ���� 
			AND A.REQUEST_ORDER_DUE_SEQ = {terms["REQUEST_ORDER_DUE_SEQ"].D} 	-- UI ���� 
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
			B.MAT_REQ_DATE, -- LT �ݿ� ��¥ 
			B.QTY_PCS, B.DESCRIPTION 
	FROM	TH_TAR_OM_ITEM_REQ_MATERIAL A WITH (NOLOCK)
			INNER JOIN 
			ITEM_MATERIAL_REQ_DATE B 
			ON A.ORDER_DATE = B.ORDER_DATE   -- ���� ��¥ ��ȹ�� �ʿ� ���� ����� �־�� �� 
			AND A.DIVISION_ID = B.DIVISION_ID
			AND A.ITEM_CODE = B.ITEM_CODE
			AND A.REVISION = B.REVISION
			LEFT OUTER JOIN 
			TH_GUI_ITEM_MODEL_SEARCH C   
			ON A.DIVISION_ID = C.DIVISION_ID
			AND A.ITEM_CODE = C.ITEM_CODE 
			AND A.REVISION = FORMAT(CONVERT(INT,C.REV), '000')	
	--WHERE	A.ORDER_DATE =(select order_date from today_date) 
	--AND A.DIVISION_ID = 'SPS'			-- UI ���� 
	--AND B.REQUEST_ORDER_ID = 4			-- UI ���� 
	--AND B.REQUEST_ORDER_DUE_SEQ = 4		-- UI ���� 
	-- AND A.ITEM_CODE = 'MCP23279B00'	-- UI �׸��� ������ ���� ITEM CODE 
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
-- ������ PNL�� ȯ�� 
OM_OP_LINE_MATERIAL_REQ_PNL as (
	SELECT	A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.MAT_REQ_DATE, A.REQ_MATERIAL_QTY,  
			A.REQ_QTY_PCS, -- ���� �ʿ� ���� PCS
			-- CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) AS PCS_PER_PNL, 
			CASE WHEN CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) > 0 
				 THEN 1.0 * REQ_QTY_PCS /( CONVERT(FLOAT, B.UP)*  CONVERT(FLOAT, B.ARRANGEMENT_X) * CONVERT(FLOAT, B.ARRANGEMENT_Y) ) ELSE NULL END AS REQ_QTY_PNL  -- -- ����ǰ �䱸���� ���� ���� �ʿ� ������ PNL ȯ�� ����
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
			isnull(A.MATERIAL_QTY, 0) as MATERIAL_QTY,  -- ���ν������� ������ �ְ� ����
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
	and		A.EVENT_DATE >= (select order_date from today_date)  -- ���� ���� ���� �����ʹ� ������
) 
,
MATERIAL_INOUT_INOUT_AVAIL_LIST as (	-- order date ���� ��ü ��¥�� ���� ���� ������ ��� 
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
		--and     A.EVENT_DATE <= (select search_date from search_date)  -- ���⼭�� ��ü �Ⱓ ������ ���� 
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
		where   A.EVENT_DATE <= (select MAT_REQ_DATE from search_date)  -- search_date ������ ������ ���뷮�� search_date�� ���뷮 
		
	) A
	where A.rnk = 1 
)
,
MATERIAL_AVAILABLE_QTY as (	
	select	A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
			--B.SEARCH_DATE, 
			B.MAT_REQ_DATE,
			--B.EVENT_DATE as last_event_date, 
			B.AVAILABLE_QTY -- ������ ������ ���� ���뷮
	from	OM_OP_LINE_MATERIAL_REQ_PNL A  
			left outer join 
			MATERIAL_INOUT_PREV_LIST B 
			on A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE
) 
,
-- 3) ���뷮�� �ʿ䷮ �̻��� MAT_REQ_DATE ������ ���� ����� ���ڸ� ���ϱ� ����, ��ü ���� ���뷮�� ��� 
MATERIAL_INOUT_FUTURE_LIST as (	-- ��ȸ �����ϰ� �� ���� �����͸� ��ȸ�ϰ�, max seq Ȯ�� 
	select * 
	from (	
		select	A.*, 
				rank() over (partition by A.MATERIAL_ITEM_CODE order by A.LIST_SEQ) as rnk 
		from ( 
			-- ������ ���� ���뷮�� ����ϸ�, �̰� ���� ������. 
			select A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
				   A.MAT_REQ_DATE as EVENT_DATE, --B.EVENT_DATE as last_event_date, 
  				   A.AVAILABLE_QTY, 0 as LIST_SEQ  -- search date ���뷮�� �ʿ䷮ ���� ���ų� ������ search date�� �������� ǥ�� (LIST_SEQ �ֿ켱����) 
			from	MATERIAL_AVAILABLE_QTY A 
			where  A.REQ_QTY_PNL <= A.AVAILABLE_QTY   -- search date ���뷮�� �ʿ䷮ ���� ���ų� ������ search date�� �������� ǥ�� 
			-- ������ ������ ���뷮 �߿��� �ʿ䷮ �̻��� �� 
			union all 
			select	
					A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
					B.EVENT_DATE, -- ���뷮�� �ʿ䷮ �̻��� SEARCH_DATE ������ ���� ����� ����  
  					B.CUMUL_BALANCE_QTY as AVAILABLE_QTY, B.LIST_SEQ    	
			from	MATERIAL_AVAILABLE_QTY A  
					inner join --left outer join 
					MATERIAL_INOUT_INOUT_AVAIL_LIST B 
					on a.MATERIAL_ITEM_CODE = b.MATERIAL_ITEM_CODE  
			--where   1=1 --material_item_code = (select material_item_code from material_item_code) --'ACF00090'
			and     B.EVENT_DATE > A.MAT_REQ_DATE -- (select search_date from search_date)
			and     A.REQ_QTY_PNL <= B.CUMUL_BALANCE_QTY  -- ���뷮�� �ʿ䷮ �̻�
			----order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ desc  -- �ݵ�� �ʿ� 
		) A
	) A 
	where A.rnk = 1 
	--order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ 
)  
select	A.ORDER_DATE, -- ������ ��ȹ���� 
		--A.ITEM_CODE, A.REVISION, 
		A.MATERIAL_ITEM_CODE, 
		A.REQ_MATERIAL_QTY,  
		A.MAT_REQ_DATE as REQ_DATE, 
        (select dbo.GET_OM_ITEM_YIELD ({terms["ITEM_CODE"].V})) AS YIELD,
        ROUND(A.REQ_QTY_PCS / (select dbo.GET_OM_ITEM_YIELD ({terms["ITEM_CODE"].V})), 2) AS REQ_QTY_PCS_YIELD,
        CEILING( (select dbo.GET_CONVERSION_QTY({terms["ITEM_CODE"].V},  (A.REQ_QTY_PCS / (select dbo.GET_OM_ITEM_YIELD ({terms["ITEM_CODE"].V}))), 'PNL')) ) AS REQ_QTY_PNL_YIELD,
		A.REQ_QTY_PCS, A.REQ_QTY_PNL, 		
		A.EVENT_DATE, -- ���뷮 ������ �����ϴ� SEARCH_DATE �Ǵ� �� ������ ���� �ֱ� ��¥ 
		A.AVAILABLE_QTY as AVAILABLE_QTY_ON_SEARCH_DATE, -- search_date ������ ���뷮 	
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
	 -- select   format(getdate(),'yyyyMMdd') as order_date  -- ���� ��¥�� �������� ��� 
    select {terms["ORDER_DATE"].V}  as order_date
), 
ITEM_MAX_REV AS (
	          SELECT  DIVISION_ID
                  , ITEM_CODE
                  , FORMAT(CONVERT(INT,MAX(REV)), '000') AS REVISION	--## 2025.09.08. ����. 1, 001 �� ���� ���� ȥ��. 
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
			--####### �Ʒ� 2�� ���� 
			A.QTY_PCS * isnull(B.REQ_MATERIAL_QTY, 1) as MATERIAL_QTY_PCS, 
			--###########  2025.09.03. �Ʒ� ���� ��ȣ �߰� (3�� �� ������) 
			case when convert(float, C.UP)*  convert(float, ARRANGEMENT_X) * convert(float, ARRANGEMENT_Y) > 0 
			     then ceiling( 1.0*A.QTY_PCS / ( convert(float, C.UP)*  convert(float, C.ARRANGEMENT_X) * convert(float, C.ARRANGEMENT_Y) ) )  else null end as MATERIAL_QTY --  MATERIAL_QTY_PNL
	from ORDER_PROMISING A 
		 inner join 
		 TH_TAR_OM_ITEM_REQ_MATERIAL B with (nolock)  -- select *  from  TH_TAR_OM_ITEM_REQ_MATERIAL with (nolock) where order_date  =  (select format(getdate(),'yyyyMMdd') as order_date ) and item_code in ('MCP23685A00') --, 'MCP23279B00' ) 
		 on A.DIVISION_ID = B.DIVISION_ID
		 and A.item_code = B.item_code 
		 and A.revision = B.revision 
		 --############ �Ʒ� �߰� 
		 left outer join 
		 TH_GUI_ITEM_MODEL_SEARCH C   -- SQM ȯ�� ����.  select top 100 * from TH_GUI_ITEM_MODEL_SEARCH B with (nolock) 	
		 on A.DIVISION_ID = C.DIVISION_ID
		 and A.item_code = C.item_code 
		 and B.revision = FORMAT(CONVERT(INT,C.REV), '000')		--## 2025.09.08. ����. -- C.REV  
		 where B.ORDER_DATE =  ({terms["ORDER_DATE"].V} )  -- ########################
)
--select * from  ORDER_PROMISING_MAT_REQ 
--insert into TH_TAR_OM_MATERIAL_INOUT_PLAN
--     (A.ORDER_DATE, A.DIVISION_ID, A.EVENT_DATE, A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, A.MATERIAL_QTY, A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.INSERT_ID, A.INSERT_DTTM, A.UPDATE_ID, A.UPDATE_DTTM)
select	B.order_date, A.DIVISION_ID, 
		--B.order_date AS EVENT_DATE, 
		--A.PROMISING_DATE AS EVENT_DATE,  --## 2025.09.08. ����  
		--> 2025.09.10. ����  LT �ݿ��ϰ�, ORDER_DATE �������� ���� �ʰ� ������ ��¥�� ���� 
		A.PROMISING_DATE, 
		--############  2025-09-10 ����   LT��ŭ ���� �ʿ������ ����, ORDER_DATE���� ������ �ʰ� 
		greatest(B.order_date, convert(date, dateadd(DAY, isnull(-C.ITEM_LT_DAY, 0), A.PROMISING_DATE) ) ) AS EVENT_DATE,  --##2025.09.10. ����  LT �ݿ��ϰ�, ORDER_DATE �������� ���� �ʰ� ������ ��¥�� ���� 
		'OUT' INOUT_GBN, 'OUTGOING' INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME,
		-1.0*A.MATERIAL_QTY as MATERIAL_QTY, -- PNL ������ 
		A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER,
		A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, 
		A.ITEM_CODE, A.MODEL_NAME, A.REVISION,
		'ADMIN' INSERT_ID, getdate() INSERT_DTTM, 'ADMIN' UPDATE_ID, getdate() UPDATE_DTTM
from	ORDER_PROMISING_MAT_REQ A  with (nolock)
		join 
		today_date B 
		on 1=1
		--############  2025-09-10 ����
		left outer join 
		TH_TAR_OM_ITEM_LEADTIME C  -- OM Lead Time -- select * from  TH_TAR_OM_ITEM_LEADTIME with (nolock) where USE_YN = 'Y' and item_code = 'BOC00994A00' 
		on A.DIVISION_ID = C.DIVISION_ID  
		AND A.ITEM_CODE = C.ITEM_CODE 
		and C.USE_YN = 'Y' 
;















WITH 
today_date as (
	-- select format(getdate(),'yyyyMMdd') as order_date  -- ���� ��¥�� �������� ��� 
	select {terms["ORDER_DATE"].V}  as order_date
),
search_date as (
	 select {terms["PROMISING_DATE"].V}  as search_date -- TH_TAR_OM_ORDER_PROMISING ���� ������ ���� PROMISING_DATE 
),
OM_OP_MATERIAL_LIST AS ( 
	SELECT	--TOP 100 --*  
			A.ORDER_DATE, A.DIVISION_ID, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, A.REQ_MATERIAL_QTY, 
			B.REQUEST_ORDER_ID, B.REQUEST_ORDER_DUE_SEQ, B.REQUEST_ORDER_DUE_SEQ_ORDER, B.TOTAL_SORT_ORDER, B.PROMISING_DATE, B.QTY_PCS, B.DESCRIPTION 
	FROM	TH_TAR_OM_ITEM_REQ_MATERIAL A WITH (NOLOCK)
			INNER JOIN 
			TH_TAR_OM_ORDER_PROMISING B WITH (NOLOCK) 
			ON A.ORDER_DATE = B.ORDER_DATE   -- ���� ��¥ ��ȹ�� �ʿ� ���� ����� �־�� �� 
			AND A.DIVISION_ID = B.DIVISION_ID
			AND A.ITEM_CODE = B.ITEM_CODE
			AND A.REVISION = B.REVISION
			LEFT OUTER JOIN 
			TH_GUI_ITEM_MODEL_SEARCH C   -- SQM ȯ�� ����.  SELECT TOP 100 * FROM TH_GUI_ITEM_MODEL_SEARCH B WITH (NOLOCK) 	
			ON A.DIVISION_ID = C.DIVISION_ID
			AND A.ITEM_CODE = C.ITEM_CODE 
			AND A.REVISION = C.REV
	WHERE	A.ORDER_DATE =(select order_date from today_date) --  (select format(getdate(),'yyyyMMdd' ))  -- ���� ��¥�� �������� ���  -- '20250904' 
	AND A.DIVISION_ID = {terms["DIVISION_ID"].V}			-- UI ���� 
	AND B.REQUEST_ORDER_ID = {terms["REQUEST_ORDER_ID"].D}			-- UI ���� 
	AND B.REQUEST_ORDER_DUE_SEQ = {terms["REQUEST_ORDER_DUE_SEQ"].D}			-- UI ���� 
	-- AND A.ITEM_CODE = {terms["ITEM_CODE"].V}	-- UI �׸��� ������ ���� ITEM CODE 
	--ORDER BY A.MATERIAL_ITEM_CODE
)--select * from OM_OP_MATERIAL_LIST
-- ������ LINE�� ���� 
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
			isnull(A.MATERIAL_QTY, 0) as MATERIAL_QTY,  -- ���ν������� ������ �ְ� ����
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
	and		A.EVENT_DATE >= convert(date, getdate())  -- ���� ���� ���� �����ʹ� ������
)
,

MATERIAL_INOUT_INOUT_AVAIL_LIST as (	-- order date ���� ��ü ��¥�� ���� ���� ������ ��� 
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
		--and     A.EVENT_DATE <= (select search_date from search_date)  -- ���⼭�� ��ü �Ⱓ ������ ���� 
		--order by A.LIST_SEQ desc  
	) A
	--where A.rnk = 1 
) -- select * from  MATERIAL_INOUT_INOUT_AVAIL_LIST  with (nolock) order by MATERIAL_ITEM_CODE, LIST_SEQ
,
-- 2) SEARCH_DATE�� ���뷮
MATERIAL_INOUT_PREV_LIST as (	-- SEARCH_DATE�� ���뷮�� �˱� ���� ��ȸ �����ϰ� �� ���� �����͸� ��ȸ�ϰ�, max seq Ȯ�� 
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
		where   A.EVENT_DATE <= (select search_date from search_date)  -- search_date ������ ������ ���뷮�� search_date�� ���뷮 
		--order by A.LIST_SEQ desc  
	) A
	where A.rnk = 1 
)
--select * from MATERIAL_INOUT_PREV_LIST; 
-- ����ǰ ���� search_date �������� ���� ���뷮 
,
MATERIAL_AVAILABLE_QTY as (
	--select * from   MATERIAL_INOUT_PREV_LIST
	--select MATERIAL_ITEM_CODE, search_date, EVENT_DATE as LAST_MATERIAL_EVENT_DATE, CUMUL_BALANCE_QTY, rnk from  MATERIAL_INOUT_PREV_LIST with (nolock) order by material_item_code, rnk
	select	A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY,  A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
			B.SEARCH_DATE, --B.EVENT_DATE as last_event_date, 
			AVAILABLE_QTY -- ������ ������ ���� ���뷮
	from	OM_OP_LINE_MATERIAL_REQ_PNL A  
			left outer join 
			MATERIAL_INOUT_PREV_LIST B 
			on A.MATERIAL_ITEM_CODE = B.MATERIAL_ITEM_CODE
) --select * from MATERIAL_AVAILABLE_QTY; 
,
-- 3) ���뷮�� �ʿ䷮ �̻��� SEARCH_DATE ������ ���� ����� ���ڸ� ���ϱ� ����, ��ü ���� ���뷮�� ��� 
MATERIAL_INOUT_FUTURE_LIST as (	-- ��ȸ �����ϰ� �� ���� �����͸� ��ȸ�ϰ�, max seq Ȯ�� 
	select * 
	from (	
		select	A.*, 
				rank() over (partition by A.MATERIAL_ITEM_CODE order by A.LIST_SEQ) as rnk 
		from ( 
			-- ������ ���� ���뷮�� ����ϸ�, �̰� ���� ������. 
			select A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
				   A.SEARCH_DATE as EVENT_DATE, --B.EVENT_DATE as last_event_date, 
  				   A.AVAILABLE_QTY, 0 as LIST_SEQ  -- search date ���뷮�� �ʿ䷮ ���� ���ų� ������ search date�� �������� ǥ�� (LIST_SEQ �ֿ켱����) 
			from	MATERIAL_AVAILABLE_QTY A 
			where  A.REQ_QTY_PNL <= A.AVAILABLE_QTY   -- search date ���뷮�� �ʿ䷮ ���� ���ų� ������ search date�� �������� ǥ�� 
			-- ������ ������ ���뷮 �߿��� �ʿ䷮ �̻��� �� 
			union all 
			select	--A.* 
					A.ITEM_CODE, A.REVISION, A.MATERIAL_ITEM_CODE, A.REQ_MATERIAL_QTY, A.REQ_QTY_PCS, A.REQ_QTY_PNL, 
					B.EVENT_DATE, -- ���뷮�� �ʿ䷮ �̻��� SEARCH_DATE ������ ���� ����� ����  
  					B.CUMUL_BALANCE_QTY as AVAILABLE_QTY, B.LIST_SEQ    	
			from	MATERIAL_AVAILABLE_QTY A  
					inner join --left outer join 
					MATERIAL_INOUT_INOUT_AVAIL_LIST B 
					on a.MATERIAL_ITEM_CODE = b.MATERIAL_ITEM_CODE  
			--where   1=1 --material_item_code = (select material_item_code from material_item_code) --'ACF00090'
			and     B.EVENT_DATE > A.search_date -- (select search_date from search_date)
			and     A.REQ_QTY_PNL <= B.CUMUL_BALANCE_QTY  -- ���뷮�� �ʿ䷮ �̻�
			----order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ desc  -- �ݵ�� �ʿ� 
		) A
	) A 
	where A.rnk = 1 
	--order by A.MATERIAL_ITEM_CODE, A.LIST_SEQ 
)  
select	A.ORDER_DATE, -- ������ ��ȹ���� 
		--A.ITEM_CODE, A.REVISION, 
		A.MATERIAL_ITEM_CODE, 
		A.REQ_MATERIAL_QTY,  
		A.SEARCH_DATE as REQ_DATE, 
		B.REQ_QTY_PCS, B.REQ_QTY_PNL, 		
		A.EVENT_DATE, -- ���뷮 ������ �����ϴ� SEARCH_DATE �Ǵ� �� ������ ���� �ֱ� ��¥ 
		A.AVAILABLE_QTY as AVAILABLE_QTY_ON_SEARCH_DATE, -- search_date ������ ���뷮 	
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


    -- �ڵ� ���� ����
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

                -- MAX �������� ǥ�� 
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

