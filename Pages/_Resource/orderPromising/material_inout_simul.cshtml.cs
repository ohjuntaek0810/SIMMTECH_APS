using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class material_inout_simul : BasePageModel
    {
        public material_inout_simul()
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
                Params terms = e.Params["terms"];

                Vali vali = new Vali(terms);
                vali.Null("material_code", "MATERIAL CODE를 입력해주세요.");
                vali.Done();

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "simul")
            {
                ParamList dataList = e.Params["data"];

                // 시뮬레이션 실행
                toClient["data"] = this.simulation();


                // 데이터 저장
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

            string date = terms["start_date"].AsString().Replace("-", "");

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
with
today_date as (
        select format(getdate(),'yyyyMMdd') as order_date  -- 당일 날짜의 기준정보 사용
),
MATERIAL_INOUT_LIST as (
        select  A.ORDER_DATE, A.DIVISION_ID, A.EVENT_DATE, A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME,
                       --A.MATERIAL_QTY,
                       --case when A.INOUT_GBN = 'IN' then isnull(A.MATERIAL_QTY, 0) when A.INOUT_GBN = 'OUT' then  -1*isnull(A.MATERIAL_QTY, 0) else 0 end as MATERIAL_QTY,
                       isnull(A.MATERIAL_QTY, 0) AS MATERIAL_QTY,
                       A.REQUEST_ORDER_ID, A.REQUEST_ORDER_SORT_ORDER, A.REQUEST_ORDER_DUE_SEQ, A.REQUEST_ORDER_DUE_SEQ_ORDER, A.ITEM_CODE, A.MODEL_NAME, A.REVISION,
                       rank ()  over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by EVENT_DATE, INOUT_GBN, case when INOUT_CATEGORY = 'INVENTORY' then 1  when INOUT_CATEGORY = 'READY_INPUT' then 2 when INOUT_CATEGORY = 'SAMPLE_SAFETY' then 3 else 9 end, INOUT_CATEGORY,REQUEST_ORDER_SORT_ORDER, REQUEST_ORDER_DUE_SEQ_ORDER)  as LIST_SEQ 
        from    TH_TAR_OM_MATERIAL_INOUT_PLAN A  with (nolock)
        where   1=1
       	and A.ORDER_DATE = '{date}'
        and A.EVENT_DATE >= convert(date, getdate())  -- 현재 일자 이전 데이터는 무시함
)
select  A.ORDER_DATE, A.DIVISION_ID, A.EVENT_DATE, A.INOUT_GBN, A.INOUT_CATEGORY, A.MATERIAL_ITEM_CODE, A.MATERIAL_ITEM_NAME, A.MATERIAL_QTY,
               A.REQUEST_ORDER_ID, A.REQUEST_ORDER_DUE_SEQ, A.ITEM_CODE, A.MODEL_NAME, A.REVISION, A.LIST_SEQ,
               sum(A.MATERIAL_QTY) over (partition by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE order by A.LIST_SEQ rows between unbounded preceding and current row) as CUMUL_BALANCE_QTY
from    MATERIAL_INOUT_LIST A
where 1=1
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
    AND A.DIVISION_ID = {terms["group_id"].V}
");
            }

            if (terms["material_code"].Length > 0)
            {
                sSQL.Append($@"
    AND A.MATERIAL_ITEM_CODE LIKE '%{terms["material_code"].AsString()}%'
");
            }

            sSQL.Append($@"
order by A.ORDER_DATE, A.DIVISION_ID, A.MATERIAL_ITEM_CODE, A.LIST_SEQ
");

            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private bool simulation()
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
                Console.WriteLine(ex.ToString());
                return false; // 실패
            }
        }



        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            //HS.Web.Proc.TH_GUI_READY_BY_INPUT_MATERIAL_REMARK.Save(data);
        }
    }
}
