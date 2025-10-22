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
    public class problem_report : BasePageModel
    {
        public problem_report()
        {
            this.Handler = handler;
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
select TOP 1
    PLAN_ID as PLAN_ID
from
    th_eng_plan_info with (NOLOCK)
            where
    master_id = 'SIMMTECH'
order by PLAN_ID desc
");

            Params result = Data.Get(sSQL.ToString()).Tables[0].ToParams();
            this.Params["first_plan_id"] = result["PLAN_ID"];
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];
                int offset = e.Params["offset"];    // 시작 위치
                int limit = e.Params["limit"];      // 한 번에 가져올 건수

                toClient["data"] = this.Search(terms, offset, limit);
                toClient["total_count"] = this.SearchTotalCount(terms);
            }

         
            return toClient;
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable Search(Params terms, int offset, int limit)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            //PROBLEM_TYPE = 'UNPLAN'  일경우 
            //노드 아이디의 ##으로 잘라서 1번째만 사용함
            //##으로 잘라진 1번째만 _로 잘리서 P1, P2 조건을 만듬, #으로 잘라진 1번째만 __ 구분으로 잘라서 뒷부분은 P3
            //TH_MST_ITEM_ROUTE_SITE 테이블과 조인 하여 현재 공정 가져옴   OUTER APPLY 조인문 사용해서 현재공정보다 LEVEL이 작은 공정 보여줌

            //PROBLEM_TYPE = 'LATE' 일경우
            //NODE_ID의 ##으로 자른 제일 우측 데이터를 APS_ENG_SITE_RESOURCE_LIST_V 의 APS_RESOURCE_ID 와 조인 하여 공정 보여줌 


            sSQL.Append($@"
SELECT
    M.*,
    R.ROUTE_LEVEL AS CURRENT_ROUTE_LEVEL,
    R.ROUTE_ID AS CURRENT_ROUTE_ID,
    PREV.ROUTE_LEVEL AS PREV_ROUTE_LEVEL,
    PREV.ROUTE_ID AS PREV_ROUTE_ID,
    (CASE WHEN PROBLEM_TYPE = 'LATE' THEN APS_RESOURCE_NAME 
         WHEN PROBLEM_TYPE = 'UNPLAN' THEN PREV.ROUTE_ID
    END)  문제공정,
    LEFT(NODE_ID, CHARINDEX('_', NODE_ID) - 1) AS ITEM_NAME,    
    (CASE PROBLEM_DESCR WHEN 'Capacity short' THEN 'Capacity 지연'
                       WHEN 'No producing link' THEN 'Routing 연결 끊김' 
                       ELSE '' END )AS PROBLEM_DESCR_NM  ,
    (CASE PROBLEM_TYPE WHEN 'LATE' THEN '납기보다 늦어짐'
                       WHEN 'UNPLAN' THEN '기준정보 누락' 
                       ELSE '' END )AS PROBLEM_TYPE_NM  
FROM 
(
    -- 노드 아이디의 앞부분을 _로 잘리서 P1,P2 조건을 만듬 , 노드 아이디를 __ 구분으로 잘라서 뒷부분은 P3
        SELECT
        A.*,
        CASE 
            WHEN CHARINDEX('_', A.SVAL) > 0 
            THEN LEFT(A.SVAL, CHARINDEX('_', A.SVAL) - 1)
            ELSE A.SVAL
        END AS P1,
        CASE 
            WHEN CHARINDEX('_', A.SVAL) > 0 AND CHARINDEX('_', A.SVAL, CHARINDEX('_', A.SVAL) + 1) > 0
            THEN SUBSTRING(
                A.SVAL,
                CHARINDEX('_', A.SVAL) + 1,
                CHARINDEX('_', A.SVAL, CHARINDEX('_', A.SVAL) + 1) - CHARINDEX('_', A.SVAL) - 1
            )
            ELSE NULL
        END AS P2,
        SUBSTRING(A.SVAL, CHARINDEX('__', A.SVAL) + 2, LEN(A.SVAL)) AS P3       
        FROM 
        (
            -- 노드아이디 ##구분으로 잘라서 앞부분만 사용
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                DEMAND_ID,
                NODE_TYPE,
                NODE_ID,
                CASE 
                    WHEN CHARINDEX('##', NODE_ID) > 0 
                    THEN LEFT(NODE_ID, CHARINDEX('##', NODE_ID) - 1)
                    ELSE NODE_ID
                END AS SVAL,
                '' AS APS_RESOURCE_NAME
            FROM TH_OUT_PROBLEM_REPORT
            WHERE  1=1 
            AND PROBLEM_TYPE = 'UNPLAN'
            AND PLAN_ID = '{terms["PLAN_ID"].AsString()}'
            ");
            if (terms["PROBLEM_TYPE"].Length > 0)
            {
                sSQL.Append($@"
                    AND PROBLEM_TYPE  = '{terms["PROBLEM_TYPE"].AsString()}'
                ");
            }
            sSQL.Append($@"
            UNION ALL 
            -- LATE 타입의 문제공정은 APS_ENG_SITE_RESOURCE_LIST_V 뷰에서 가져옴 
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                DEMAND_ID,
                NODE_TYPE,
                NODE_ID,
                RIGHT(NODE_ID, CHARINDEX('##', REVERSE(NODE_ID)) - 1) AS SVAL, -- NODE_ID의## 으로 잘라서 제일 우측 
                R.APS_RESOURCE_NAME as APS_RESOURCE_NAME
            FROM TH_OUT_PROBLEM_REPORT T
            LEFT OUTER JOIN APS_ENG_SITE_RESOURCE_LIST_V R
            ON RIGHT(T.NODE_ID, CHARINDEX('##', REVERSE(T.NODE_ID)) - 1) = R.APS_RESOURCE_ID  
            WHERE 1=1 
            AND PROBLEM_TYPE = 'LATE'
            AND PLAN_ID = '{terms["PLAN_ID"].AsString()}'  ");
            if (terms["PROBLEM_TYPE"].Length > 0)
            {
                sSQL.Append($@"
                    AND PROBLEM_TYPE  = '{terms["PROBLEM_TYPE"].AsString()}'
                ");
            }
            sSQL.Append($@"           
        ) A
) M

LEFT OUTER JOIN TH_MST_ITEM_ROUTE_SITE R 
      ON R.CORPORATION = 'STK' 
      AND R.PLANT = 'STK' 
      AND R.MASTER_ID = 'SIMMTECH'
      AND R.IRS_ATTB_2 = M.P1 
      AND R.IRS_ATTB_9 = M.P2 
      AND R.ROUTE_ID = M.P3
      AND R.PLAN_ID = M.PLAN_ID
      AND M.PROBLEM_TYPE = 'UNPLAN'
OUTER APPLY ( 
    -- 아우터 어플라이를 사용해서 개별조건걸어 현재레벨보다 작은것 top 1 의 라우트 번호를 가져옴
    SELECT TOP 1 P.ROUTE_ID,P.ROUTE_LEVEL
    FROM TH_MST_ITEM_ROUTE_SITE P
    WHERE P.CORPORATION = R.CORPORATION
      AND P.PLANT = R.PLANT
      AND P.MASTER_ID = R.MASTER_ID
      AND P.IRS_ATTB_2 = R.IRS_ATTB_2
      AND P.IRS_ATTB_9 = R.IRS_ATTB_9
      AND P.PLAN_ID = R.PLAN_ID
      AND P.ROUTE_LEVEL < R.ROUTE_LEVEL
      AND M.PROBLEM_TYPE = 'UNPLAN'
    ORDER BY P.ROUTE_LEVEL DESC
) PREV
WHERE M.PLAN_ID = '{terms["PLAN_ID"].AsString()}'
ORDER BY M.PLAN_ID DESC
OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY
");
            Console.WriteLine(sSQL.ToString());
            
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchTotalCount(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            //PROBLEM_TYPE = 'UNPLAN'  일경우 
            //노드 아이디의 ##으로 잘라서 1번째만 사용함
            //##으로 잘라진 1번째만 _로 잘리서 P1, P2 조건을 만듬, #으로 잘라진 1번째만 __ 구분으로 잘라서 뒷부분은 P3
            //TH_MST_ITEM_ROUTE_SITE 테이블과 조인 하여 현재 공정 가져옴   OUTER APPLY 조인문 사용해서 현재공정보다 LEVEL이 작은 공정 보여줌

            //PROBLEM_TYPE = 'LATE' 일경우
            //NODE_ID의 ##으로 자른 제일 우측 데이터를 APS_ENG_SITE_RESOURCE_LIST_V 의 APS_RESOURCE_ID 와 조인 하여 공정 보여줌 


            sSQL.Append($@"
SELECT
    count(*)
FROM 
(
    -- 노드 아이디의 앞부분을 _로 잘리서 P1,P2 조건을 만듬 , 노드 아이디를 __ 구분으로 잘라서 뒷부분은 P3
        SELECT
        A.*,
        CASE 
            WHEN CHARINDEX('_', A.SVAL) > 0 
            THEN LEFT(A.SVAL, CHARINDEX('_', A.SVAL) - 1)
            ELSE A.SVAL
        END AS P1,
        CASE 
            WHEN CHARINDEX('_', A.SVAL) > 0 AND CHARINDEX('_', A.SVAL, CHARINDEX('_', A.SVAL) + 1) > 0
            THEN SUBSTRING(
                A.SVAL,
                CHARINDEX('_', A.SVAL) + 1,
                CHARINDEX('_', A.SVAL, CHARINDEX('_', A.SVAL) + 1) - CHARINDEX('_', A.SVAL) - 1
            )
            ELSE NULL
        END AS P2,
        SUBSTRING(A.SVAL, CHARINDEX('__', A.SVAL) + 2, LEN(A.SVAL)) AS P3       
        FROM 
        (
            -- 노드아이디 ##구분으로 잘라서 앞부분만 사용
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                DEMAND_ID,
                NODE_TYPE,
                NODE_ID,
                CASE 
                    WHEN CHARINDEX('##', NODE_ID) > 0 
                    THEN LEFT(NODE_ID, CHARINDEX('##', NODE_ID) - 1)
                    ELSE NODE_ID
                END AS SVAL,
                '' AS APS_RESOURCE_NAME
            FROM TH_OUT_PROBLEM_REPORT
            WHERE  1=1 
            AND PROBLEM_TYPE = 'UNPLAN'
            AND PLAN_ID = '{terms["PLAN_ID"].AsString()}'
            ");
            if (terms["PROBLEM_TYPE"].Length > 0)
            {
                sSQL.Append($@"
                    AND PROBLEM_TYPE  = '{terms["PROBLEM_TYPE"].AsString()}'
                ");
            }
            sSQL.Append($@"
            UNION ALL 
            -- LATE 타입의 문제공정은 APS_ENG_SITE_RESOURCE_LIST_V 뷰에서 가져옴 
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                DEMAND_ID,
                NODE_TYPE,
                NODE_ID,
                RIGHT(NODE_ID, CHARINDEX('##', REVERSE(NODE_ID)) - 1) AS SVAL, -- NODE_ID의## 으로 잘라서 제일 우측 
                R.APS_RESOURCE_NAME as APS_RESOURCE_NAME
            FROM TH_OUT_PROBLEM_REPORT T
            LEFT OUTER JOIN APS_ENG_SITE_RESOURCE_LIST_V R
            ON RIGHT(T.NODE_ID, CHARINDEX('##', REVERSE(T.NODE_ID)) - 1) = R.APS_RESOURCE_ID  
            WHERE 1=1 
            AND PROBLEM_TYPE = 'LATE'
            AND PLAN_ID = '{terms["PLAN_ID"].AsString()}'  ");
            if (terms["PROBLEM_TYPE"].Length > 0)
            {
                sSQL.Append($@"
                    AND PROBLEM_TYPE  = '{terms["PROBLEM_TYPE"].AsString()}'
                ");
            }
            sSQL.Append($@"           
        ) A
) M

LEFT OUTER JOIN TH_MST_ITEM_ROUTE_SITE R 
      ON R.CORPORATION = 'STK' 
      AND R.PLANT = 'STK' 
      AND R.MASTER_ID = 'SIMMTECH'
      AND R.IRS_ATTB_2 = M.P1 
      AND R.IRS_ATTB_9 = M.P2 
      AND R.ROUTE_ID = M.P3
      AND R.PLAN_ID = M.PLAN_ID
      AND M.PROBLEM_TYPE = 'UNPLAN'
OUTER APPLY ( 
    -- 아우터 어플라이를 사용해서 개별조건걸어 현재레벨보다 작은것 top 1 의 라우트 번호를 가져옴
    SELECT TOP 1 P.ROUTE_ID,P.ROUTE_LEVEL
    FROM TH_MST_ITEM_ROUTE_SITE P
    WHERE P.CORPORATION = R.CORPORATION
      AND P.PLANT = R.PLANT
      AND P.MASTER_ID = R.MASTER_ID
      AND P.IRS_ATTB_2 = R.IRS_ATTB_2
      AND P.IRS_ATTB_9 = R.IRS_ATTB_9
      AND P.PLAN_ID = R.PLAN_ID
      AND P.ROUTE_LEVEL < R.ROUTE_LEVEL
      AND M.PROBLEM_TYPE = 'UNPLAN'
    ORDER BY P.ROUTE_LEVEL DESC
) PREV
WHERE M.PLAN_ID = '{terms["PLAN_ID"].AsString()}'
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }
    }
}
/*
SELECT
    M.*,
    R.ROUTE_LEVEL AS CURRENT_ROUTE_LEVEL,
    R.ROUTE_ID AS CURRENT_ROUTE_ID,
    PREV.ROUTE_LEVEL AS PREV_ROUTE_LEVEL,
    PREV.ROUTE_ID AS PREV_ROUTE_ID,
    (CASE WHEN PROBLEM_TYPE = 'LATE' THEN APS_RESOURCE_NAME 
         WHEN PROBLEM_TYPE = 'UNPLAN' THEN PREV.ROUTE_ID
    END)  문제공정
FROM 
(
    -- 노드 아이디의 앞부분을 _로 잘리서 P1,P2 조건을 만듬 , 노드 아이디를 __ 구분으로 잘라서 뒷부분은 P3
    SELECT
        A.*,
        CASE 
            WHEN CHARINDEX('_', A.SVAL) > 0 
            THEN LEFT(A.SVAL, CHARINDEX('_', A.SVAL) - 1)
            ELSE A.SVAL
        END AS P1,
        CASE 
            WHEN CHARINDEX('_', A.SVAL) > 0 AND CHARINDEX('_', A.SVAL, CHARINDEX('_', A.SVAL) + 1) > 0
            THEN SUBSTRING(
                A.SVAL,
                CHARINDEX('_', A.SVAL) + 1,
                CHARINDEX('_', A.SVAL, CHARINDEX('_', A.SVAL) + 1) - CHARINDEX('_', A.SVAL) - 1
            )
            ELSE NULL
        END AS P2,
        SUBSTRING(A.SVAL, CHARINDEX('__', A.SVAL) + 2, LEN(A.SVAL)) AS P3       
        FROM 
        (
            -- 노드아이디 ##구분으로 잘라서 앞부분만 사용
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                NODE_TYPE,
                NODE_ID,
                CASE 
                    WHEN CHARINDEX('##', NODE_ID) > 0 
                    THEN LEFT(NODE_ID, CHARINDEX('##', NODE_ID) - 1)
                    ELSE NODE_ID
                END AS SVAL,
                '' AS APS_RESOURCE_NAME
            FROM TH_OUT_PROBLEM_REPORT
            WHERE  1=1 
            AND PROBLEM_TYPE = 'UNPLAN'
            AND PLAN_ID = 'SIMM_20250806_P04'
            UNION ALL 
            -- LATE 타입의 문제공정은 APS_ENG_SITE_RESOURCE_LIST_V 뷰에서 가져옴 
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                NODE_TYPE,
                NODE_ID,
                RIGHT(NODE_ID, CHARINDEX('##', REVERSE(NODE_ID)) - 1) AS SVAL, -- NODE_ID의## 으로 잘라서 제일 우측 
                R.APS_RESOURCE_NAME as APS_RESOURCE_NAME
            FROM TH_OUT_PROBLEM_REPORT T
            LEFT OUTER JOIN APS_ENG_SITE_RESOURCE_LIST_V R
            ON RIGHT(T.NODE_ID, CHARINDEX('##', REVERSE(T.NODE_ID)) - 1) = R.APS_RESOURCE_ID  
            WHERE 1=1 
            AND PROBLEM_TYPE = 'LATE'
            AND PLAN_ID = 'SIMM_20250806_P04' 
           
        ) A
) M

LEFT OUTER JOIN TH_MST_ITEM_ROUTE_SITE R 
    ON R.CORPORATION = 'STK' 
    AND R.PLANT = 'STK' 
    AND R.MASTER_ID = 'SIMMTECH'
    AND R.IRS_ATTB_2 = M.P1 
    AND R.IRS_ATTB_9 = M.P2 
    AND R.ROUTE_ID = M.P3
    AND R.PLAN_ID = M.PLAN_ID
    AND M.PROBLEM_TYPE = 'UNPLAN'
OUTER APPLY ( 
    -- 아우터 어플라이를 사용해서 개별조건걸어 현재레벨보다 작은것 top 1 의 라우트 번호를 가져옴
    SELECT TOP 1 P.ROUTE_ID,P.ROUTE_LEVEL
    FROM TH_MST_ITEM_ROUTE_SITE P
    WHERE P.CORPORATION = R.CORPORATION
      AND P.PLANT = R.PLANT
      AND P.MASTER_ID = R.MASTER_ID
      AND P.IRS_ATTB_2 = R.IRS_ATTB_2
      AND P.IRS_ATTB_9 = R.IRS_ATTB_9
      AND P.PLAN_ID = R.PLAN_ID
      AND P.ROUTE_LEVEL < R.ROUTE_LEVEL
      AND M.PROBLEM_TYPE = 'UNPLAN'
    ORDER BY P.ROUTE_LEVEL DESC
) PREV
WHERE M.PLAN_ID = 'SIMM_20250806_P04'
ORDER BY M.PLAN_ID DESC



*/