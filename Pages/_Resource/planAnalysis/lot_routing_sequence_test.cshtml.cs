//using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class lot_routing_sequence_test : BasePageModel
    {

        public lot_routing_sequence_test()
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

                vali.Null("PLAN_ID", "PLAN_ID가 입력되지 않았습니다.");

                vali.DoneDeco();

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("WRK_CLS_CD", "작업분류코드가 입력되지 않았습니다.");
                vali.Null("WRK_CLS_NM", "작업분류명이 입력되지 않았습니다.");
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

            string yyyymmdd = terms["PLAN_ID"].AsString().Substring(5,8); // 날짜 구해오기
            // TODO : 추후 yyyymmdd에 값으로 넣어야함 

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
WITH 
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
WIP as (
        select W.JOB_ID, W.JOB_NAME, W.ITEM_CODE, W.REVISION, W.WORKING_TYPE as LOT_STATUS, W.OPERATION_SEQ_NUM
        from TH_TAR_WIP_HIS W
		where 1=1
	    and yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
		and SEQ = (select WIP_SEQ from cufoff_Date)
		and USE_YN = 'Y'
),
WIP_ROUTING as (
        select  JOB_NAME, ITEM_CODE, OPERATION_SEQ_NUM, DEPT_CODE, DEPT_NAME, QUANTITY
        from    th_tar_wip_routing_his
        where  1=1
        and yyyymmdd = (select WIP_YYYYMMDD from cufoff_Date) 
		and SEQ = (select WIP_SEQ from cufoff_Date)
    --yyyymmdd = '20250911'
),
Order_Tracking as (
	SELECT  A.DEMAND_ID, A.TRACK_SEQ, A.OPERATION_SEQ, A.SITE_ID, A.ROUTE_ID, A.OPERATION_ID, A.RESOURCE_ID, 
			A.OPERATION_QTY, 
			A.START_TIME, A.END_TIME, 
			A.AVAILABLE_TIME 
	FROM TH_OUT_ORDER_TRACKING	A with (nolock)	
	WHERE	A.MASTER_ID = 'SIMMTECH'		-- 'SIMMTECH'		
	AND		A.PLAN_ID = {terms["PLAN_ID"].V}			-- 'SIM_20250630_004' 
	AND		A.OUT_VERSION_ID = {terms["PLAN_ID"].V}	-- 'SIM_20250630_004'
	--and demand_id = 'MS22N0291-05' --'AH2530018-01'  -- 임시 
	--and route_id is not null 
	and ORDER_ID_TYPE = 'W'
	--order by A.DEMAND_ID, A.OPERATION_SEQ, A.start_time
), 
APS_RESOURCE_MAP as (
	select RESOURCE_CAPA_GROUP_ID, RESOURCE_CAPA_GROUP_NAME, APS_RESOURCE_ID, APS_RESOURCE_NAME, OWN_OUT_GBN
	from APS_ENG_SITE_RESOURCE_LIST_V
	--where APS_RCG_CAPA_REV = (select RCG_CAPA_REV from TH_TAR_RCG_CAPA_H where use_yn = 'Y' ) 
	--and use_yn = 'Y' 
)
select	
		'SIMMTECH' AS MASTER_ID, 
        {terms["PLAN_ID"].V} AS PLAN_ID, 
		W.ITEM_CODE, 
        W.REVISION AS REV,
        W.JOB_ID, 
        W.JOB_NAME, 
		case when W.OPERATION_SEQ_NUM = WR.OPERATION_SEQ_NUM then W.LOT_STATUS else '' end as LOT_STATUS, 
		WR.OPERATION_SEQ_NUM, 
		--RM.RESOURCE_CAPA_GROUP_ID, 
		RM.RESOURCE_CAPA_GROUP_NAME AS RES_CAPA_GROUP_NAME, -- 공정그룹 대신 Resource Capa Group
		WR.DEPT_CODE, WR.DEPT_NAME, -- 세부공정 
		case when RM.OWN_OUT_GBN = 'SIMMTECH' then DM.SITE_ID 
			 when RM.OWN_OUT_GBN = 'OUTSOURCE' then 'OUTSOURCE' 
			 else '' end as SITE, 
		--WR.QUANTITY
		-- 실적 없음
		--MA.PRODUCT_M2, MA.MAC_CODE, MA.MACHINE,  
		--MA.CLOCK_ACCEPT, MA.CLOCK_IN, MA.CLOCK_END, MA.CLOCK_OUT,		
		OT.RESOURCE_ID AS RES_ID, 
        RM.APS_RESOURCE_NAME AS APS_RES_NAME, 		
		RM.OWN_OUT_GBN,  -- 자사 외주 구분
		OT.OPERATION_QTY, 
        FORMAT(OT.START_TIME, 'yyyy-MM-dd') AS ""PLAN DATE"", 
        ROUND(DATEDIFF(MINUTE, OT.START_TIME, OT.END_TIME) / 60.0, 2) AS ""LEAD TIME"",
--        DATEDIFF(hour, OT.START_TIME, OT.END_TIME) AS ""LEAD TIME"", 
        FORMAT(OT.START_TIME, 'yyyy-MM-dd HH:mm') AS ""START TIME"", 
        FORMAT(OT.END_TIME, 'yyyy-MM-dd HH:mm') AS ""END TIME"",
        LAG(OT.END_TIME) OVER (
		    PARTITION BY W.JOB_NAME
		    ORDER BY WR.OPERATION_SEQ_NUM
		) AS PREV_END_TIME,		
		ROUND(
		    DATEDIFF(MINUTE, 
		        LAG(OT.END_TIME) OVER (
		            PARTITION BY W.JOB_NAME
		            ORDER BY WR.OPERATION_SEQ_NUM
		        ), 
		        OT.END_TIME
		    ) / 60.0, 2
		) AS LEAD_TIME_WITH_WAIT
from	WIP W 
		left outer join 
		WIP_ROUTING WR
		on W.JOB_NAME = WR.JOB_NAME
		left outer join 		
		Order_Tracking OT 
		on W.JOB_NAME = OT.DEMAND_ID
		AND WR.OPERATION_SEQ_NUM * 10 = OT.OPERATION_SEQ
		left outer join
		APS_RESOURCE_MAP RM 
		on OT.RESOURCE_ID = RM.APS_RESOURCE_ID
		left outer join 
		TH_TAR_DEPT_MASTER DM 
		on WR.DEPT_CODE =  DM.DEPARTMENT_CODE	
where WR.DEPT_CODE not in ('V0001', 'V9999') 
");

            /*
             * 조건절 시작
             */

            // item_code
            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
    AND W.ITEM_CODE like '%{terms["item_code"].AsString()}%'
");
            }

            // srch_lot_id
            if (terms["srch_lot_id"].Length > 0)
            {
                sSQL.Append($@"
    AND W.JOB_NAME like '%{terms["srch_lot_id"].AsString()}%'
");
            }

            sSQL.Append(@"
order by W.ITEM_CODE, WR.JOB_NAME, WR.OPERATION_SEQ_NUM -- A.DEMAND_ID, A.start_time 
");

            
            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];

            //string[] mergeCols = new[] { "ITEM_CODE", "JOB_ID" };

            //DataTable result = Data.Get("MES", sSQL.ToString()).Tables[0];

            //FormatForCellMerge(result, mergeCols);

            //return result;

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

        // CELL 병합처럼 보이기
        public static DataTable FormatForCellMerge(DataTable dt, string[] mergeColumns)
        {
            if (dt.Rows.Count == 0) return dt;

            foreach (string colName in mergeColumns)
            {
                string prevValue = dt.Rows[0][colName]?.ToString();

                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    string currentValue = dt.Rows[i][colName]?.ToString();

                    if (currentValue == prevValue)
                    {
                        dt.Rows[i][colName] = DBNull.Value; // 같은 값이면 null 처리
                    }
                    else
                    {
                        prevValue = currentValue; // 새로운 값이면 업데이트
                    }
                }
            }

            return dt;
        }
    }
}
