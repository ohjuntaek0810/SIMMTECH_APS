using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class item_route_attribute_drill : BasePageModel
    {
        public item_route_attribute_drill()
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
SELECT 
	im.ITEM_ID 
	, im.REVISION_ID
	, im.CATEGORY_ID
	, im.LAYER
	, im.MFG_CATEGORY
	, im.ORDER_TYPE
	, im.CUSTOMER_ID
	, im.ITEM_NAME AS MODEL_NAME
	, im.CUSTOMIZED 
	, im.SUBPRODUCT
	, im.MSAP_TENTING
	, im.PATTERN_TYPE_ID
	, im.YIELD
	, im.DEFAULT_LOT_SIZE
--	, im.th --TODO : thick 부터 하위컬럼 필요
from 
	TH_TAR_ITEM_MASTER im
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
