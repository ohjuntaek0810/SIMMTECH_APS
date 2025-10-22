using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class monthly_delivery_capa_input : BasePageModel
    {
        public monthly_delivery_capa_input()
        {
            this.Handler = handler;

            var sSQL = new StringBuilder();          

            Params result = Data.Get("select dbo.GET_OM_CAPA_MONTHS() AS OM_CAPA_MONTHS ").Tables[0].ToParams();
          
            this.Params["OM_CAPA_MONTHS"] = result["OM_CAPA_MONTHS"];

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
            else if (e.Command == "save")
            {
                Params terms = e.Params["terms"];
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                this.Save(dataList);
                toClient["data"] = this.SearchBasic(terms);

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


            // DateTime current = new DateTime(startDate.Year, startDate.Month, 1);
            // DateTime end = new DateTime(endDate.Year, endDate.Month, 1);

            List<string> dateList = new List<string>();

            //월단위 증가 
            for (DateTime date = startDate; date <= endDate; date = date.AddMonths(1))
            {
                dateList.Add($"[{date:yyyy-MM-dd}]");
            }

            string result = string.Join(", ", dateList);

            var sSQL = new StringBuilder();

            sSQL.Append($@"

WITH
-- 현재 기준 8개월 목록 
MONTHS AS (
    SELECT DATEADD(MONTH, N, CAST(CAST(YEAR(GETDATE()) AS VARCHAR) + '-' + CAST(MONTH(GETDATE()) AS VARCHAR)+ '-01' AS DATE)) AS MONTH
    FROM (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)) AS N(N)
), 
MONTH_CAPA_LIST AS (
    SELECT    A.DIVISION_ID, A.GROUP_GUBUN_ID, A.BOTTLE_NECK_ID, A.GROUP_GUBUN_NAME, A.BOTTLE_NECK_NAME,
            B.MONTH AS MONTH_DATE, 
            FORMAT(B.MONTH, 'yyyy-MM') AS MONTH_YM,
            DAY(EOMONTH(B.MONTH)) AS MONTH_DAYS
    FROM    OM_BOTTLE_NECK_LIST_V A WITH (NOLOCK)
            CROSS JOIN MONTHS B 
    where 1=1
    ");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
            }

            sSQL.Append($@"	
),
JOINED_DATA AS (
    SELECT  A.*, 
            B.MONTH_CAPA, 
            B.MONTH_CAPA * 1.0 / A.MONTH_DAYS AS DAILY_CAPA
    FROM    MONTH_CAPA_LIST A
            LEFT JOIN TH_TAR_OM_BOTTLE_NECK_CAPA_L B
            ON A.DIVISION_ID = B.DIVISION_ID 
            AND A.GROUP_GUBUN_ID = B.GROUP_GUBUN_ID
            AND A.BOTTLE_NECK_ID = B.BOTTLE_NECK_ID
            AND A.MONTH_DATE = B.MONTH_DATE 
    WHERE 1=1 
");
			 if (terms["group_id"].Length > 0)
             {
                sSQL.Append($@"
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
             }
            sSQL.Append($@"	
)
SELECT *
FROM (
    SELECT DIVISION_ID, GROUP_GUBUN_ID, BOTTLE_NECK_ID, GROUP_GUBUN_NAME, BOTTLE_NECK_NAME, MONTH_DATE, MONTH_CAPA --isnull(MONTH_CAPA,0)as MONTH_CAPA
    FROM JOINED_DATA
) AS SourceTable
PIVOT (
    SUM(MONTH_CAPA)
    FOR MONTH_DATE IN ({result})
) AS PivotResult

ORDER BY DIVISION_ID, GROUP_GUBUN_ID,BOTTLE_NECK_ID,GROUP_GUBUN_NAME,BOTTLE_NECK_NAME




");

            return Data.Get(sSQL.ToString()).Tables[0];
        }



        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();

            ParamList pl_save_row = new ParamList();
            foreach (Params row in dataList)
            {
                // 공통 메타 정보 추출
                string divisionId = row["DIVISION_ID"];
                string groupGubunId = row["GROUP_GUBUN_ID"];
                string bottleNeckId = row["BOTTLE_NECK_ID"];
                string groupGubunName = row["GROUP_GUBUN_NAME"];
                string bottleNeckName = row["BOTTLE_NECK_NAME"];

                foreach (var col in row)
                {
                    string col_key = col.Key;
                    // yyyy-MM-dd 형식인지 확인
                    if (DateTime.TryParseExact(col.Key, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    {
                        Params newRow = new Params();
                        newRow["DIVISION_ID"] = divisionId;
                        newRow["GROUP_GUBUN_ID"] = groupGubunId;
                        newRow["BOTTLE_NECK_ID"] = bottleNeckId;
                        newRow["GROUP_GUBUN_NAME"] = groupGubunName;
                        newRow["BOTTLE_NECK_NAME"] = bottleNeckName;
                        newRow["MONTH_date"] = col.Key;
                        newRow["MONTH_CAPA"] = col.Value.ToString();
						pl_save_row.Add(newRow);
                    }
                }
            }

            pl_save_row.ForEach(ITEM =>
            {
                sSQL.Append($@"
						MERGE INTO TH_TAR_OM_BOTTLE_NECK_CAPA_L AS TARGET
						USING (SELECT 
									{ITEM["DIVISION_ID"].V} AS DIVISION_ID,
									{ITEM["GROUP_GUBUN_ID"].V} AS GROUP_GUBUN_ID,
									{ITEM["GROUP_GUBUN_NAME"].V} AS GROUP_GUBUN_NAME,
									{ITEM["BOTTLE_NECK_ID"].V} AS BOTTLE_NECK_ID,
									{ITEM["BOTTLE_NECK_NAME"].V} AS BOTTLE_NECK_NAME,
									{ITEM["MONTH_date"].V} AS MONTH_date,
									{ITEM["MONTH_CAPA"].V} AS MONTH_CAPA,                                    
                                    {ITEM["MONTH_CAPA"].D} * 1.0 / DAY(EOMONTH({ITEM["MONTH_date"].V}))  AS DAILY_CAPA,
                                    DAY(EOMONTH({ITEM["MONTH_date"].V})) AS MONTH_DAYS   
							   ) AS SOURCE
						ON TARGET.DIVISION_ID = SOURCE.DIVISION_ID
						   AND TARGET.GROUP_GUBUN_ID = SOURCE.GROUP_GUBUN_ID
						   AND TARGET.GROUP_GUBUN_NAME = SOURCE.GROUP_GUBUN_NAME
						   AND TARGET.BOTTLE_NECK_ID = SOURCE.BOTTLE_NECK_ID
						   AND TARGET.BOTTLE_NECK_NAME = SOURCE.BOTTLE_NECK_NAME
						   AND TARGET.MONTH_date = SOURCE.MONTH_date
						WHEN MATCHED THEN
							UPDATE SET TARGET.MONTH_CAPA = SOURCE.MONTH_CAPA , TARGET.DAILY_CAPA = SOURCE.DAILY_CAPA , TARGET.MONTH_DAYS = SOURCE.MONTH_DAYS
						WHEN NOT MATCHED THEN
							INSERT (DIVISION_ID, GROUP_GUBUN_ID, GROUP_GUBUN_NAME, BOTTLE_NECK_ID, BOTTLE_NECK_NAME, MONTH_date, MONTH_CAPA,DAILY_CAPA,MONTH_DAYS)
							VALUES (SOURCE.DIVISION_ID, SOURCE.GROUP_GUBUN_ID, SOURCE.GROUP_GUBUN_NAME, SOURCE.BOTTLE_NECK_ID, SOURCE.BOTTLE_NECK_NAME, SOURCE.MONTH_date, SOURCE.MONTH_CAPA,SOURCE.DAILY_CAPA,SOURCE.MONTH_DAYS);
					");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());

        }

    }
}

