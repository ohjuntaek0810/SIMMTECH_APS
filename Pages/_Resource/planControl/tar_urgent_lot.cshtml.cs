using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class tar_urgent_lot : BasePageModel
    {

        public tar_urgent_lot()
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

            else if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {

                Params Searchterms = e.Params["Searchterms"];
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                this.Save(dataList);
                toClient["TermsResult"] = this.Search(Searchterms);
            }

            else if (e.Command == "expirysave")
            {

                Params Searchterms = e.Params["Searchterms"];
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                this.ExpirySave(dataList);
                toClient["TermsResult"] = this.Search(Searchterms);
            }




            else if (e.Command == "delete")
            {
                ParamList data = e.Params["data"];


                this.delete(data);
            }

            else if (e.Command == "init_ExpirySave")
            {
                // 데이터 저장
                this.init_ExpirySave();                
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
-- 긴급도 1,2,3 코드값 룩업에서 이름 찾아서 표시
urgent_level_list as (
	select	SEGMENT1 as URGENT_LEVEL,			-- DB 입력 데이터 
			ATTRIBUTE02 as URGENT_LEVEL_NAME,	-- UI 표시 데이터 
			sort_order  -- 목록 정렬용
	from LOOKUP_VALUE_M where LOOKUP_TYPE_CODE = 'LT_REDUCTION_RATE_BY_URGENCY_LEVEL' 
	and LOOKUP_TYPE_VERSION = ( select LOOKUP_TYPE_VERSION 
								from LOOKUP_TYPE_M 
								where LOOKUP_TYPE_CODE = 'LT_REDUCTION_RATE_BY_URGENCY_LEVEL' 
								and active_flag = 'Y' ) 
	and ACTIVE_FLAG = 'Y' 
	--order by sort_order  
)
select	--A.YYYYMMDD, A.SEQ, 
		B.URGENCY_LEVEL  as URGENT_LEVEL,
		D.URGENT_LEVEL_NAME, 
		B.EXPIRY_DATE, 
		A.DIVISION_ID,
        E.CUSTOMER_NAME,
        E.MODEL_NAME,
        A.ITEM_CODE,
		A.JOB_NAME AS LOT_NO, 
		C.DEPARTMENT_NAME, 
		A.OPERATION_SEQ_NUM as STEP, --A.RESOURCE_CODE, A.RESOURCE_NAME, 
		FORMAT(CONVERT(datetime, A.SCH_DATE, 3), 'yyyy-MM-dd') SCH_DATE, --A.FIRST_UNIT_START_DATE, 
	    FORMAT(
			    DATEFROMPARTS(
			      2000 + TRY_CAST(LEFT(A.COMP_DATE, 2) AS int),
			      TRY_CAST(SUBSTRING(A.COMP_DATE, 4, 2) AS int),
			      TRY_CAST(RIGHT(A.COMP_DATE, 2) AS int)
			    ),
			    'yyyy-MM-dd'
		      ) AS COMP_DATE ,
		A.PRODUCT_WPNL, A.PRODUCT_PCS, A.PRODUCT_M2, -- A.SUBCONTRACTOR_FLAG, A.REPEAT, A.INNER_OUTER, A.MFG_CATEGORY, A.PATTERN, A.INPUT_YIELD, A.NEXT_OPERATION_SEQ_NUM, A.URGENCY_STATUS, 
		B.DESCRIPTION as DESCRIPTION, 
		A.INSERT_ID, A.INSERT_DTTM, A.UPDATE_ID, A.UPDATE_DTTM,
        A.WORKING_TYPE,
        A.WORK_STATUS,
        A.ORGANIZATION_ID,
        A.DEPT_CODE,
        A.REVISION,
        A.JOB_ID, 
        A.JOB_NAME,
        B.DIVISION_ID,
        B.URGENCY_LEVEL,
        B.USE_YN AS USE_YN
from	TH_TAR_WIP A 
        LEFT OUTER JOIN TH_TAR_URGENT_LOT  B	
        ON A.JOB_NAME = B.job_name  
");
            sSQL.Append($@"
		LEFT OUTER JOIN	TH_TAR_DEPT_MASTER C	ON A.DEPT_CODE = C.DEPARTMENT_CODE 
		LEFT OUTER JOIN urgent_level_list D		ON B.URGENCY_LEVEL = D.URGENT_LEVEL
        LEFT OUTER JOIN TH_GUI_ITEM_BY_PROCESS_GUBUN E ON A.ITEM_CODE = E.ITEM_CODE
where	1=1
");
            if (terms["use_yn"].Length > 0)
            {
                sSQL.Append($@"
                    AND B.USE_YN  = {terms["use_yn"].V}
                ");
            }

            if (terms["URGENCY_LEVEL"].Length > 0)
            {
                sSQL.Append($@"
                    AND B.URGENCY_LEVEL  = '{terms["URGENCY_LEVEL"].AsString()}'
                ");
            }

            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
            }
            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
                ");
            }

            if (terms["customer"].Length > 0)
            {
                sSQL.Append($@"
                    AND E.CUSTOMER_NAME LIKE '%{terms["customer"].AsString()}%'
                ");
            }

            if (terms["LOTNO"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.JOB_NAME LIKE '%{terms["LOTNO"].AsString()}%'
                ");
            }
            sSQL.Append($@"
   ORDER BY A.OPERATION_SEQ_NUM DESC 
");

            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {

            StringBuilder sSQL = new StringBuilder();

            data.ForEach(ITEM =>
            {
                sSQL.Append($@"                   
MERGE INTO TH_TAR_URGENT_LOT AS target
USING (
    SELECT 
        {ITEM["ORGANIZATION_ID"].V} AS ORGANIZATION_ID,
        {ITEM["DIVISION_ID"].V} AS DIVISION_ID,
        {ITEM["JOB_ID"].V} AS JOB_ID,
        {ITEM["JOB_NAME"].V} AS JOB_NAME,
        {ITEM["URGENCY_LEVEL"].V} AS URGENCY_LEVEL,
        {ITEM["EXPIRY_DATE"].V} AS EXPIRY_DATE,
        {ITEM["DESCRIPTION"].V} AS DESCRIPTION,
        {ITEM["USE_YN"].V} AS USE_YN,
        {ITEM["ITEM_CODE"].V} AS ITEM_CODE,
        {ITEM["REVISION"].V} AS REVISION,
        {ITEM["INSERT_ID"].V} AS INSERT_ID,       
        {ITEM["UPDATE_ID"].V} AS UPDATE_ID      
) AS source
ON target.ORGANIZATION_ID = source.ORGANIZATION_ID AND target.JOB_ID = source.JOB_ID AND target.JOB_NAME = source.JOB_NAME
WHEN MATCHED THEN 
    UPDATE SET 
        target.ORGANIZATION_ID = source.ORGANIZATION_ID,
        target.DIVISION_ID = source.DIVISION_ID,
        target.URGENCY_LEVEL = source.URGENCY_LEVEL,
        target.EXPIRY_DATE = source.EXPIRY_DATE,
        target.DESCRIPTION = source.DESCRIPTION,    
        target.USE_YN = source.USE_YN,
        target.ITEM_CODE = source.ITEM_CODE,
        target.REVISION = source.REVISION,
        target.UPDATE_ID = source.UPDATE_ID,
        target.UPDATE_DTTM = GETDATE() 
WHEN NOT MATCHED THEN 
    INSERT (
        ORGANIZATION_ID, DIVISION_ID, JOB_ID, JOB_NAME, URGENCY_LEVEL, EXPIRY_DATE,
        DESCRIPTION, USE_YN, ITEM_CODE, REVISION, INSERT_ID, INSERT_DTTM,
        UPDATE_ID, UPDATE_DTTM
    )
    VALUES (
        source.ORGANIZATION_ID, source.DIVISION_ID, source.JOB_ID, source.JOB_NAME, source.URGENCY_LEVEL, source.EXPIRY_DATE,
        source.DESCRIPTION, 'Y', source.ITEM_CODE, source.REVISION, source.INSERT_ID, GETDATE() ,
        null, null
    );
                ");
            });


            HS.Web.Common.Data.Execute(sSQL.ToString());

        }

        /// <summary>
        /// 해제
        /// </summary>
        /// <param name="data"></param>
        private void ExpirySave(ParamList data)
        {

            StringBuilder sSQL = new StringBuilder();

            data.ForEach(ITEM =>
            {
                sSQL.Append($@"
              UPDATE TH_TAR_URGENT_LOT SET                 
                     URGENCY_LEVEL = null,
                     EXPIRY_DATE = null,
                     DESCRIPTION = null,    
                     USE_YN = 'N'                 
               WHERE ORGANIZATION_ID = {ITEM["ORGANIZATION_ID"].V} 
                 AND JOB_ID = {ITEM["JOB_ID"].V} 
                 AND JOB_NAME = {ITEM["JOB_NAME"].V}  ; 
            ");

            });


            HS.Web.Common.Data.Execute(sSQL.ToString());

        }

        private void init_ExpirySave()
        {

            StringBuilder sSQL = new StringBuilder();
          
                sSQL.Append($@"

UPDATE TH_TAR_URGENT_LOT
SET    
    URGENCY_LEVEL = null,
    EXPIRY_DATE = null,
    DESCRIPTION = null,    
    USE_YN = 'N'              
WHERE EXPIRY_DATE <= CAST(GETDATE() AS DATE) and  USE_YN != 'N';
            ");


            HS.Web.Common.Data.Execute(sSQL.ToString());

        }

        /// <summary>
        /// 선택한 항목 삭제
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            StringBuilder sSQL = new StringBuilder();
            data.ForEach(ITEM =>
            {
                sSQL.Append($@"
               DELETE FROM TH_TAR_URGENT_LOT        
               WHERE ORGANIZATION_ID = {ITEM["ORGANIZATION_ID"].V} 
                 AND JOB_ID = {ITEM["JOB_ID"].V} 
                 AND JOB_NAME = {ITEM["JOB_NAME"].V}  
                ;");
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

                return HS.Core.Excel.Download(dtResult, "Resource_Master_" + timestamp);
            }
            else
                return Page();
        }
    }
}
