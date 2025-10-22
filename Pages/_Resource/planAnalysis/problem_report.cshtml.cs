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
                int offset = e.Params["offset"];    // ���� ��ġ
                int limit = e.Params["limit"];      // �� ���� ������ �Ǽ�

                toClient["data"] = this.Search(terms, offset, limit);
                toClient["total_count"] = this.SearchTotalCount(terms);
            }

         
            return toClient;
        }

        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable Search(Params terms, int offset, int limit)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            //PROBLEM_TYPE = 'UNPLAN'  �ϰ�� 
            //��� ���̵��� ##���� �߶� 1��°�� �����
            //##���� �߶��� 1��°�� _�� �߸��� P1, P2 ������ ����, #���� �߶��� 1��°�� __ �������� �߶� �޺κ��� P3
            //TH_MST_ITEM_ROUTE_SITE ���̺�� ���� �Ͽ� ���� ���� ������   OUTER APPLY ���ι� ����ؼ� ����������� LEVEL�� ���� ���� ������

            //PROBLEM_TYPE = 'LATE' �ϰ��
            //NODE_ID�� ##���� �ڸ� ���� ���� �����͸� APS_ENG_SITE_RESOURCE_LIST_V �� APS_RESOURCE_ID �� ���� �Ͽ� ���� ������ 


            sSQL.Append($@"
SELECT
    M.*,
    R.ROUTE_LEVEL AS CURRENT_ROUTE_LEVEL,
    R.ROUTE_ID AS CURRENT_ROUTE_ID,
    PREV.ROUTE_LEVEL AS PREV_ROUTE_LEVEL,
    PREV.ROUTE_ID AS PREV_ROUTE_ID,
    (CASE WHEN PROBLEM_TYPE = 'LATE' THEN APS_RESOURCE_NAME 
         WHEN PROBLEM_TYPE = 'UNPLAN' THEN PREV.ROUTE_ID
    END)  ��������,
    LEFT(NODE_ID, CHARINDEX('_', NODE_ID) - 1) AS ITEM_NAME,    
    (CASE PROBLEM_DESCR WHEN 'Capacity short' THEN 'Capacity ����'
                       WHEN 'No producing link' THEN 'Routing ���� ����' 
                       ELSE '' END )AS PROBLEM_DESCR_NM  ,
    (CASE PROBLEM_TYPE WHEN 'LATE' THEN '���⺸�� �ʾ���'
                       WHEN 'UNPLAN' THEN '�������� ����' 
                       ELSE '' END )AS PROBLEM_TYPE_NM  
FROM 
(
    -- ��� ���̵��� �պκ��� _�� �߸��� P1,P2 ������ ���� , ��� ���̵� __ �������� �߶� �޺κ��� P3
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
            -- �����̵� ##�������� �߶� �պκи� ���
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
            -- LATE Ÿ���� ���������� APS_ENG_SITE_RESOURCE_LIST_V �信�� ������ 
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                DEMAND_ID,
                NODE_TYPE,
                NODE_ID,
                RIGHT(NODE_ID, CHARINDEX('##', REVERSE(NODE_ID)) - 1) AS SVAL, -- NODE_ID��## ���� �߶� ���� ���� 
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
    -- �ƿ��� ���ö��̸� ����ؼ� �������ǰɾ� ���緹������ ������ top 1 �� ���Ʈ ��ȣ�� ������
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
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchTotalCount(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            //PROBLEM_TYPE = 'UNPLAN'  �ϰ�� 
            //��� ���̵��� ##���� �߶� 1��°�� �����
            //##���� �߶��� 1��°�� _�� �߸��� P1, P2 ������ ����, #���� �߶��� 1��°�� __ �������� �߶� �޺κ��� P3
            //TH_MST_ITEM_ROUTE_SITE ���̺�� ���� �Ͽ� ���� ���� ������   OUTER APPLY ���ι� ����ؼ� ����������� LEVEL�� ���� ���� ������

            //PROBLEM_TYPE = 'LATE' �ϰ��
            //NODE_ID�� ##���� �ڸ� ���� ���� �����͸� APS_ENG_SITE_RESOURCE_LIST_V �� APS_RESOURCE_ID �� ���� �Ͽ� ���� ������ 


            sSQL.Append($@"
SELECT
    count(*)
FROM 
(
    -- ��� ���̵��� �պκ��� _�� �߸��� P1,P2 ������ ���� , ��� ���̵� __ �������� �߶� �޺κ��� P3
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
            -- �����̵� ##�������� �߶� �պκи� ���
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
            -- LATE Ÿ���� ���������� APS_ENG_SITE_RESOURCE_LIST_V �信�� ������ 
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                DEMAND_ID,
                NODE_TYPE,
                NODE_ID,
                RIGHT(NODE_ID, CHARINDEX('##', REVERSE(NODE_ID)) - 1) AS SVAL, -- NODE_ID��## ���� �߶� ���� ���� 
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
    -- �ƿ��� ���ö��̸� ����ؼ� �������ǰɾ� ���緹������ ������ top 1 �� ���Ʈ ��ȣ�� ������
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
    END)  ��������
FROM 
(
    -- ��� ���̵��� �պκ��� _�� �߸��� P1,P2 ������ ���� , ��� ���̵� __ �������� �߶� �޺κ��� P3
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
            -- �����̵� ##�������� �߶� �պκи� ���
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
            -- LATE Ÿ���� ���������� APS_ENG_SITE_RESOURCE_LIST_V �信�� ������ 
            SELECT 
                PROBLEM_TYPE,
                PLAN_ID,              
                PROBLEM_DESCR,
                PROBLEM_QTY,
                NODE_TYPE,
                NODE_ID,
                RIGHT(NODE_ID, CHARINDEX('##', REVERSE(NODE_ID)) - 1) AS SVAL, -- NODE_ID��## ���� �߶� ���� ���� 
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
    -- �ƿ��� ���ö��̸� ����ؼ� �������ǰɾ� ���緹������ ������ top 1 �� ���Ʈ ��ȣ�� ������
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