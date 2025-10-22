using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class bom : BasePageModel
    {
        public bom()
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

                toClient["data"] = this.Search(terms);
            }

            if (e.Command == "search_detail")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchDetail(terms);
            }

            if (e.Command == "search_model")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchModel(terms);
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
                vali.Null("WRK_CLS_CD", "작업분류코드가 입력되지 않았습니다.");
                vali.Null("WRK_CLS_NM", "작업분류명이 입력되지 않았습니다.");
                //vali.Null("VIEW_YN", "보이기 여부가 입력되지 않았습니다.");

                vali.DoneDeco();

                this.Save(data);


                // 데이터 저장
            }

            if (e.Command == "delete")
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

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
SELECT	H.item_code AS ""ITEM CODE"", 
        H.revision AS ""REVISION"", 
        -- H.bom_header_id, 
		AC.CUSTOMER_NAME as Customer, 
		CSB.model_name as Model, 
		L.SEQ, -- sorting용. UI 미표시  
		L.OPERATION_SEQ,  
		RL.DEPARTMENT_NAME, 
        L.QUANTITY, 
		ccl.description as TYPE, 
		L.COMPONENT_ITEM_CODE AS CODE, 
		M.DESCRIPTION as NAME, 
		L.THICK		
FROM	cbst_spec_bom_h H
		inner join 
		cbst_spec_bom_l L
		on H.ORGANIZATION_ID = L.ORGANIZATION_ID
		and H.BOM_HEADER_ID = L.BOM_HEADER_ID
		left outer join 
		CBST_SPEC_BASIC CSB
		on H.item_code = CSB.item_code
		and H.revision = csb.revision
		left outer join 
		AR_CUSTOMERS AC  -- 테이블 인터페이스 안함 
		on csb.customer = ac.customer_number
		left outer join 
		TH_TAR_ROUTING_H RH
		on H.ITEM_CODE = RH.ITEM_CODE
		and H.REVISION = RH.REVISION
		and RH.ALTERNATE_DESIGNATOR_CODE = 'Primary'
		left outer join
		TH_TAR_ROUTING_L RL 
		on RH.RT_HEADER_ID = RL.RT_HEADER_ID
		and L.OPERATION_SEQ = RL.OPERATION_SEQ 
		left outer join 
		cbst_common_h cch
		on 1=1 
		and cch.lookup_type  = 'Layer type'
		left outer join
        cbst_common_l ccl
		on cch.lookup_type = ccl.lookup_type 
		and ccl.enabled_flag = 'Y'
		and L.Layer_code = ccl.line_code
		left outer join 
		MTL_SYSTEM_ITEMS_B M
		on L.organization_id = M.organization_id
		and L.COMPONENT_ITEM_ID = M.inventory_item_id 		
WHERE  1 = 1
and H.organization_id = 101
");
//            if (terms["group_id"] > 0)
//            {
//                sSQL.Append(@$"
//and H.revision = {terms["revision"].V}
//");
//            }

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append(@$"
and H.item_code LIKE '%{terms["item_code"].AsString()}%'
");
            }

            if (terms["revision"].Length > 0)
            {
                sSQL.Append($@"
and H.revision = {terms["revision"].V}
");
            }

            sSQL.Append(@"
order by H.bom_header_id, L.SEQ, H.item_code
");

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

            return Data.Get(sSQL.ToString()).Tables[0];
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


            return Data.Get(sSQL.ToString()).Tables[0];
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


        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            if (command == "ExcelDownload")
            {
                //데이터 조회한 값으로 엑셀 다운로드
                DataTable dtResult = this.Search(e.Params["terms"]);

                return HS.Core.Excel.Download(dtResult, "TestExcel");
            }
            else
                return Page();
        }
    }
}
