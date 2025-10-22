using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class delivery_capaload : BasePageModel
    {
        public delivery_capaload()
        {
            this.Handler = handler;
            Params result = HS.Web.Common.ApsManage.searchPlanId().ToParams();
            //  Console.WriteLine(result["PLAN_ID"].AsString());

            this.Params["first_plan_id"] = result["PLAN_ID"];
            Params result2 = Data.Get("select dbo.GET_OM_CAPA_MONTHS() AS OM_CAPA_MONTHS ").Tables[0].ToParams();
            this.Params["OM_CAPA_MONTHS"] = result2["OM_CAPA_MONTHS"];
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchBasic(terms);

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

            List<string> slecdateconList = new List<string>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateList.Add($"[{date:yyyy-MM-dd}]");

                slecdateconList.Add($", CEILING([{date:yyyy-MM-dd}]) AS [{date:yyyy-MM-dd}]"); //결과는 올림 처리
            }

            string result = string.Join(", ", dateList);
            string col_result = string.Join(" ",slecdateconList);
            var sSQL = new StringBuilder();

            sSQL.Append($@"
                SELECT 

                    BOTTLE_NECK_ID
                    ,BOTTLE_NECK_NAME
                    ,GROUP_GUBUN_ID
                    ,GROUP_GUBUN_NAME
                    ,CATEGORY_ID
                    ,CATEGORY_NAME
                    ,V_SORT_ORDER
                    {col_result}

                FROM (
                    SELECT 
                        A.ORDER_DATE,
                        A.DIVISION_ID,
                        A.GROUP_GUBUN_ID,
                        A.GROUP_GUBUN_NAME,
                        A.BOTTLE_NECK_ID,
                        A.BOTTLE_NECK_NAME,
                        A.PLAN_DATE,
                        A.CATEGORY_ID,
                        A.CATEGORY_VALUE,
                        D.CATEGORY_NAME CATEGORY_NAME,
                        D.SORT_ORDER,
                        V.SORT_ORDER AS V_SORT_ORDER
                        FROM TH_TAR_OM_DAILY_CAPA_RESULT A
                        LEFT OUTER JOIN OM_DAILY_CAPA_CATEGORY_V D ON A.CATEGORY_ID = D.CATEGORY_ID
                        LEFT OUTER JOIN OM_BOTTLE_NECK_LIST_V V ON A.DIVISION_ID = V.DIVISION_ID AND A.BOTTLE_NECK_ID = V.BOTTLE_NECK_ID

                 WHERE 1=1 
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                    AND A.ORDER_DATE = '{terms["ORDER_DATE"].AsString().Replace("-","")}'
                        ");        
            sSQL.Append($@"
                ) AS SourceTable
                PIVOT (
                    SUM(CATEGORY_VALUE)
                    FOR PLAN_DATE IN ({result})
                ) AS PivotResult
                ORDER BY V_SORT_ORDER
                ;
              ");

            return Data.Get(sSQL.ToString()).Tables[0];
        }


    }
}

