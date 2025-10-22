using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class bor : BasePageModel
    {
        public bor()
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
WITH 
--DIVISION_LIST as 
--(
--	SELECT SEGMENT1 AS DIVISION_ID, ATTRIBUTE01 AS DIVISION_NAME 
--	FROM LOOKUP_VALUE_M WHERE LOOKUP_TYPE_CODE = 'DIVISION_LIST' 
--) 
DEPT_CLASS AS (
	   SELECT DEPARTMENT_CLASS_CODE AS DEPT_CLASS_CODE, DESC1 AS DEPT_CLASS_NAME FROM TH_TAR_DEPT_MASTER B GROUP BY DEPARTMENT_CLASS_CODE, DESC1 
),
RESOURCE_CAPA_GROUP AS (
	SELECT SEGMENT1 AS RESOURCE_CAPA_GROUP_ID, ATTRIBUTE01 AS RESOURCE_CAPA_GROUP_NAME 
	FROM LOOKUP_VALUE_M WHERE LOOKUP_TYPE_CODE = 'RESOURCE_CAPA_GROUP' 
)
SELECT	--A.CORPORATION_ID, 
		A.DIVISION_ID AS DIVISION, -- G.DIVISION_NAME, -- 원래 NAME 표시. DIVISION ID 와 NAME 동일 
		A.DEPT_CLASS_CODE AS CLASS_CODE, 
		B.DEPT_CLASS_NAME AS CLASS_NAME, 
		A.DEPT_CODE, 
		C.DESC2 AS DEPT_NAME, 
		--A.RESOURCE_CAPA_GROUP_ID,		-- 설계에 없던 부분 추가 
		--F.RESOURCE_CAPA_GROUP_NAME,	-- 설계에 없던 부분 추가 
		A.RESOURCE_CODE, 
		E.MACHINE_NAME AS RESOURCE_NAME,
		A.CUSTOMER_ID AS CUSTOMER, --  추후 name 으로 변경? 
		A.ITEM_ID AS ITEM_CODE, 
		D.ITEM_NAME,
		A.REVISION_ID, 
		A.PROCESS_TIME, 
		A.TACT_TIME, 
		A.APS_PROCESS_TIME, 
		A.APS_TACT_TIME, 
		A.ECIM_PROCESS_TIME, 
		A.ECIM_TACT_TIME, 
		A.ECIM_DOWN_TIME, 
		A.ECIM_RECIPE_ID AS ECIM_RECIPE, 
		A.USE_YN, 
		format(A.UPDATE_DTTM, 'yyyy-MM-dd') AS UPDATE_DATE, 
		A.UPDATE_ID
  FROM	TH_TAR_BOR A
		LEFT OUTER JOIN 
		DEPT_CLASS B
	    ON A.DEPT_CLASS_CODE = B.DEPT_CLASS_CODE
		LEFT OUTER JOIN 
		TH_TAR_DEPT_MASTER C
	    ON A.DIVISION_ID = C.DIVISION_ID
		AND A.DEPT_CODE = C.DEPARTMENT_CODE
		LEFT OUTER JOIN 
		TH_TAR_ITEM_MASTER D
		ON A.DIVISION_ID = D.DIVISION_ID
		AND A.ITEM_ID = D.ITEM_ID 
		AND A.REVISION_ID = D.REVISION_ID  -- 추가? 
		LEFT OUTER JOIN 
		TH_TMP_RESOURCE E
	    ON A.RESOURCE_CODE = E.MACHINE_CODE
		--LEFT OUTER JOIN 
		--RESOURCE_CAPA_GROUP F
		--ON A.RESOURCE_CAPA_GROUP_ID = F.RESOURCE_CAPA_GROUP_ID
		--LEFT OUTER JOIN 
		--DIVISION_LIST G
		--ON A.DIVISION_ID = G.DIVISION_ID
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
