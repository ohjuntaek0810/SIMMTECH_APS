using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class routing : BasePageModel
    {
        public routing()
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                Vali vali = new Vali(terms);
                vali.Null("item_code", "ITEM_CODE�� �Է����ּ���.");

                vali.DoneDeco();

                toClient["data"] = this.Search(terms);
            }

            if (e.Command == "search_model")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchModel(terms);
            }

            if (e.Command == "search_recipe_list")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchRecipeList(terms);
            }

            if (e.Command == "search_recipe_lov")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchRecipeLov(terms);
            }

            if (e.Command == "search_bom")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchBOM(terms);
            }


            if (e.Command == "search_detail")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchDetail(terms);
            }


            else if (e.Command == "search_chart")
            {
                Params terms = e.Params["terms"];

                toClient = this.search_chart(terms);
            }

            else if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("WRK_CLS_CD", "�۾��з��ڵ尡 �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("WRK_CLS_NM", "�۾��з����� �Էµ��� �ʾҽ��ϴ�.");
                //vali.Null("VIEW_YN", "���̱� ���ΰ� �Էµ��� �ʾҽ��ϴ�.");

                vali.DoneDeco();

                this.Save(data);


                // ������ ����
            }

            if (e.Command == "delete")
            {
                ParamList data = e.Params["data"];


                this.delete(data);
            }

            return toClient;
        }

        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable Search(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            String itemId = terms["item_code"];

            DataTable result_h = null;
            // �ֽ� REVISION�� ��ȸ�ϵ��� ����
            if (terms["latest_rev"] == true)
            {
                StringBuilder sSQL2 = new StringBuilder();

                sSQL2.Append(@$"
select 
	ITEM_CODE, 
	REVISION
from 
	TH_TAR_ROUTING_H 
WHERE 
	1=1
AND LAST_REV_YN = 'Y'
");
                if ( itemId.Length > 0 )
                {
                    sSQL2.Append($@"
AND	ITEM_CODE LIKE '%{itemId}%'
");
                }
                sSQL2.Append($@"
ORDER BY ITEM_CODE, REVISION DESC
");

                result_h = Data.Get(sSQL2.ToString()).Tables[0];
            }

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
-- UI ���� 
select	-- A.ORGANIZATION_ID, 
		--ITEM_CODE, REVISION ����? 
		C.ITEM_CODE AS ""ITEM CODE"", 
        C.REVISION AS ""REV"", 
		A.RT_HEADER_ID AS ""RT HEADER ID"", -- UI ��ǥ�� 
		A.OPERATION_SEQ AS ""SEQ"", 
		--A.DEPARTMENT_ID, -- UI ��ǥ�� 
		B.SITE_ID, 
        B.DEPARTMENT_CODE AS ""DEPARTMENT CODE"", 
        B.DEPARTMENT_NAME AS ""DEPARTMENT NAME"", --  Dept master���� �����ͼ� ǥ�� 		
		A.QTY, -- �����ε� ���ڿ��� ����ִ� ��. �������� ��ȯ�ؼ� �ֱ�. 
		A.W_UNIT, 
		'Y' USE_YN,		
		--A.CREATION_DATE, A.CREATED_BY, A.LAST_UPDATE_DATE, A.LAST_UPDATED_BY, A.LAST_UPDATE_LOGIN, A.RT_FLAG,
		--LAYERING_DEPT_YN, 
		LAYER_INOUT AS ""IN/OUT"", 
        THICKNESS AS ""W' THK"", 
        PPG_THICK AS ""PPG THK"", 
        HOLE_TYPE_NAME AS ""HOLE TYPE"", 
        PATTERN_CU_TYPE AS ""CU TYPE"", 
        TRACE, 
        WSIZE_X, 
        WSIZE_Y, 
		A.DESC_1, 
        A.DESC_2
from	TH_TAR_ROUTING_L A
		inner join 
		TH_TAR_ROUTING_H C
		on A.RT_HEADER_ID = C.RT_HEADER_ID
		left outer join 
		TH_TAR_DEPT_MASTER B
		on A.DEPARTMENT_ID = B.DEPARTMENT_ID
where	
	A.ORGANIZATION_ID  = 101 
and		C.USE_YN = 'Y' 

");

            // �ֽ� REVISION�� ��ȸ�ϵ��� ����
            if (terms["latest_rev"] == true)
            {
                if (result_h == null) // �������� ����� ���ٸ� ����
                {
                    return null;
                }

                string currentValue = "";

                for (int i = 0; i < result_h.Rows.Count; i++)
                {
                    
                    if (i==0)
                    {
                        sSQL.Append($@"
AND (
        (C.ITEM_CODE = '{result_h.Rows[i]["ITEM_CODE"]}' AND C.REVISION = '{result_h.Rows[i]["REVISION"]}')
");
                    } else { // �ι�° ���ǿ������ʹ� ���� ITEM_CODE�� ���� �ʴ°͸� �����ؾ���.
                        if (currentValue != result_h.Rows[i]["ITEM_CODE"]?.ToString())
                        {
                            sSQL.Append($@"
        OR (C.ITEM_CODE = '{result_h.Rows[i]["ITEM_CODE"]}' AND C.REVISION = '{result_h.Rows[i]["REVISION"]}')
");
                        }
                    }
                    currentValue = result_h.Rows[i]["ITEM_CODE"]?.ToString();
                }

                    sSQL.Append($@"
)
");
            } else
            {
                // �ֽ� REVISION üũ �ȵǾ����� ���
                if (itemId.Length > 0)
                {
                    sSQL.Append($@"
        AND C.ITEM_CODE LIKE '%{itemId}%'
");
                }

                if (terms["revision"].Length > 0)
                {
                    sSQL.Append($@"
        AND C.REVISION LIKE '%{terms["revision"].AsString()}%'
");
                }
            }


            

            sSQL.Append(@"
order by C.REVISION, C.ITEM_CODE, A.OPERATION_SEQ
");
            //string[] mergeCols = new[] { "ITEM_CODE" , "REVISION" };

            //Console.WriteLine(sSQL.ToString());

            DataTable result = Data.Get(sSQL.ToString()).Tables[0];

            //FormatForCellMerge(result, mergeCols);

            Console.WriteLine(sSQL.ToString());

            return result;
        }

        private DataTable SearchModel(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
	MODEL_NAME 
from 
	CBST_SPEC_BASIC 
where 
    item_code = {terms["item_code"].V}
    and REVISION = {terms["revision"].V}
");

            Console.WriteLine(sSQL.ToString());
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchRecipeList(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
with 
 A as (
	select * from openquery(ERP_RUN, '
            select 
                * 
            from 
                APPS.CBST_SPEC_ROUTING_RECIPE_A_V2 
            where 
                organization_id = 101 
                and  RT_HEADER_ID = ''{terms["RT HEADER ID"].AsString()}'' 
                and seq = {terms["SEQ"].AsString()} '
    )
) 
select	CODE_A_NAME as CODE_NAME, --VALUE_TYPE, 
		TYPE_NAME, VALUE_A, VALUE_B, VALUE_C
		--, REMARKS1, REMARKS2, REMARKS3
from A order by seq, recipe_code_a  
");

            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchRecipeLov(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
with 
A as (
	select * from openquery(ERP_RUN, 'select * from APPS.CBST_SPEC_ROUTING_RECIPE_B_V where organization_id = 101 and  RT_HEADER_ID = ''{terms["RT HEADER ID"].AsString()}'' and seq = {terms["SEQ"].AsString()} ')
) 
select	RT_HEADER_ID, SEQ, -- DEPARTMENT_ID, DEPARTMENT_CODE, DEPT_NAME, 
		CODE_B_NAME as CODE_NAME, 
		TYPE_CODE as TYPE_NAME, --TYPE_CLASS, 
		TYPE_CODE_NAME as VALUE_A -- , REMARKS1
from A 
order by seq, RECIPE_CODE_B 
");
            Console.WriteLine(sSQL.ToString());
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchBOM(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@$"
WITH 
BOM_LIST AS (
    -- CCL, PPG, CF ������ SPEC_BOM
    SELECT  A.BOM_HEADER_ID, A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, 
            B.SEQ, -- ����. UI ǥ�� �� ������
            B.LAYER_CODE, -- ���̾� ����. ���� ����  
            B.COMPONENT_ITEM_ID, 
			B.OPERATION_SEQ, -- ���� ���� 
			B.QUANTITY, B.THICK, 
            C.SEGMENT1 AS COMPONENT_ITEM_CODE, C.DESCRIPTION AS COMPONENT_ITEM_NAME,
            (   SELECT  CCL.DESCRIPTION -- ���� Ÿ�� 
                FROM   CBST_COMMON_L      CCL
                WHERE  CCL.LOOKUP_TYPE  = 'LAYER TYPE'
                AND    CCL.ENABLED_FLAG = 'Y'
                AND    CCL.LINE_CODE    = B.LAYER_CODE --CBST_SPEC_BOM_L.LAYER_CODE
            ) AS LAYER_TYPE,
			D.RT_HEADER_ID
    FROM    CBST_SPEC_BOM_H A WITH (NOLOCK) 
			LEFT OUTER JOIN 
            --CBST_SPEC_BOM_L B
			CBST_SPEC_BOM_L B WITH (NOLOCK) 
			ON A.ORGANIZATION_ID = B.ORGANIZATION_ID
            AND A.BOM_HEADER_ID = B.BOM_HEADER_ID
            LEFT OUTER JOIN 
            MTL_SYSTEM_ITEMS_B C WITH (NOLOCK) 
            ON A.ORGANIZATION_ID = C.ORGANIZATION_ID
			AND A.BOM_HEADER_ID = B.BOM_HEADER_ID
            AND B.COMPONENT_ITEM_ID = C.INVENTORY_ITEM_ID
			INNER JOIN 
			CBST_SPEC_ROUTING_H D WITH (NOLOCK) 
			ON A.ORGANIZATION_ID = D.ORGANIZATION_ID
			AND A.ITEM_CODE = D.ITEM_CODE
			AND A.REVISION = D.REVISION 
    WHERE   1 = 1
    AND A.ORGANIZATION_ID = 101
    --AND A.ITEM_CODE ='MCP20609H00'
    --AND A.REVISION ='000'
--	AND D.RT_HEADER_ID = 188345  -- RT_HEADER_ID�� ���� ITEM_CODE, REVISION ����
)
SELECT -- RT_HEADER_ID, ITEM_CODE, REVISION, 
		OPERATION_SEQ, LAYER_TYPE, COMPONENT_ITEM_CODE, COMPONENT_ITEM_NAME
FROM BOM_LIST
WHERE RT_HEADER_ID = '{terms["RT HEADER ID"].AsString()}'  -- RT_HEADER_ID�� ���� ITEM_CODE, REVISION ����
AND OPERATION_SEQ = '{terms["SEQ"].AsString()}'
ORDER BY OPERATION_SEQ, LAYER_TYPE, COMPONENT_ITEM_CODE
");
            Console.WriteLine(sSQL.ToString());
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchDetail(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
WITH TB_ROUTING AS (
    SELECT 
        r.ITEM_ID AS ItemNo,
        r.ROUTE_SEQ AS Seq,
        r.DEPT_CODE AS Code,
        r.DEPT_NAME AS Department,
        ROW_NUMBER() OVER (PARTITION BY r.ITEM_ID ORDER BY r.ROUTE_SEQ) AS RowNum
    FROM TH_TAR_ROUTING r
    WHERE 
    	1=1 
");
            String itemId = terms["ITEM_ID"];
            if (itemId != null)
            {
                sSQL.Append($@"
        AND r.ITEM_ID = '{itemId}'
");
            }

            sSQL.Append(@"
)
SELECT 
    CASE WHEN RowNum = 1 THEN ItemNo ELSE NULL END AS ItemNo,
    Seq,
    Code,
    Department
FROM TB_ROUTING
ORDER BY Seq
");
            Console.WriteLine(sSQL.ToString());
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private Params search_chart(Params terms)
        {
            Params result = new();

            return result;
        }


        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            //HS.Web.Proc.SAF_WRK_CLS.Save(data);
        }

        /// <summary>
        /// ������ �׸� ����
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            throw new Exception("�غ����Դϴ�.");

            StringBuilder sSQL = new StringBuilder();

            data.ForEach(D =>
            {
                sSQL.Append($@"
DELETE FROM SI_CODE_GROUP WHERE CMP_CD = {D["CMP_CD"].V} AND GRP_CD = {D["GRP_CD"].V};
");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }


        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            string itemId = Request.Form["itemId"];  // POST ������ �ޱ�
            this.Params["ItemId"] = itemId;  // ��� �� ����

            if (command == "ExcelDownload")
            {
                //������ ��ȸ�� ������ ���� �ٿ�ε�
                DataTable dtResult = this.Search(e.Params["terms"]);

                return HS.Core.Excel.Download(dtResult, "TestExcel");
            }
            else
                return Page();
        }

        // CELL ����ó�� ���̱�
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
                        dt.Rows[i][colName] = DBNull.Value; // ���� ���̸� null ó��
                    }
                    else
                    {
                        prevValue = currentValue; // ���ο� ���̸� ������Ʈ
                    }
                }
            }

            return dt;
        }
    }
}
