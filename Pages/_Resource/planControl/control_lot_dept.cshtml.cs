using DocumentFormat.OpenXml.Drawing;
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
    public class control_lot_dept : BasePageModel
    {

        public control_lot_dept()
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

            if (e.Command == "Search_Grid_Input")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search_Grid_Input(terms);
            }

            else if (e.Command == "Save_Grid_Input")
            {

                Params Searchterms = e.Params["Searchterms"];
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                this.Save(dataList);
                toClient["TermsResult"] = this.Search(Searchterms);
                toClient["TermsResultInput"] = this.Search_Grid_Input(Searchterms);
            }

            else if (e.Command == "delete")
            {
                Params Searchterms = e.Params["Searchterms"];
                ParamList data = e.Params["data"];
                this.delete(data);
                toClient["TermsResultInput"] = this.Search_Grid_Input(Searchterms);

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

            sSQL.Append($@"
   SELECT 
            WIP.YYYYMMDD,
            WIP.SEQ,
            WIP.ITEM_CODE,
            WIP.REVISION,
            WIP.JOB_ID,
            WIP.JOB_NAME,
            WIP.WORKING_TYPE,
            WIP.WORK_STATUS,
            WIP.ORGANIZATION_ID,
            WIP.DEPT_CODE,
            WIP.OPERATION_SEQ_NUM,
            WIP.RESOURCE_CODE,
            WIP.RESOURCE_NAME,
            WIP.DIVISION_ID,
            WIP.SCH_DATE,
            WIP.FIRST_UNIT_START_DATE,
            WIP.COMP_DATE,
            WIP.COMP_DATE2,
            WIP.DELTA,
            WIP.WAIT_TIME,
            WIP.SQM,
            WIP.PRODUCT_WPNL,
            WIP.PRODUCT_PCS,
            WIP.PRODUCT_M2,
            WIP.SUBCONTRACTOR_FLAG,
            WIP.REPEAT,
            WIP.INNER_OUTER,
            WIP.MFG_CATEGORY,
            WIP.PATTERN,
            WIP.INPUT_YIELD,
            WIP.NEXT_OPERATION_SEQ_NUM,
            WIP.URGENCY_STATUS,
            WIP.USE_YN,
            WIP.INSERT_ID,
            WIP.INSERT_DTTM,
            WIP.UPDATE_ID,
            WIP.UPDATE_DTTM,
            I.MODEL_NAME  AS MODEL_NAME ,           
            I.CUSTOMER  ,
            AC.CUSTOMER_NAME,
            DEPT.SITE_ID,
            MODEL_NAME,
            DM.DEPARTMENT_NAME as DEPT_NAME
           
    FROM 
            TH_TAR_WIP WIP   
    LEFT OUTER JOIN TH_TAR_DEPT_MASTER DEPT ON WIP.DEPT_CODE = DEPT.DEPARTMENT_CODE
    LEFT OUTER JOIN CBST_SPEC_BASIC I ON  WIP.ITEM_CODE = I.ITEM_CODE  AND WIP.REVISION = I.REVISION  and WIP.ORGANIZATION_ID = I.ORGANIZATION_ID
    LEFT OUTER JOIN AR_CUSTOMERS AC ON I.CUSTOMER = AC.CUSTOMER_NUMBER
    LEFT OUTER JOIN TH_TAR_DEPT_MASTER DM  ON WIP.DEPT_CODE = DM.DEPARTMENT_CODE

   WHERE 
        1=1
");

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
        AND WIP.ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
            }

            if (terms["REVISION"].Length > 0)
            {
                sSQL.Append($@"
        AND WIP.REVISION = '{terms["REVISION"].AsString()}'
");
            }

            if (terms["LOTNO"].Length > 0)
            {
                sSQL.Append($@"
        AND WIP.JOB_NAME LIKE '%{terms["LOTNO"].AsString()}%'
");
            }

            sSQL.Append($@"
   ORDER BY WIP.OPERATION_SEQ_NUM DESC 
");

            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];
        }



        private DataTable Search_Grid_Input(Params terms)
        {
            DTClient.UserInfoMerge(terms);
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"
SELECT 
        A.ORGANIZATION_ID,
        A.DIVISION_ID,
        A.JOB_ID,
        A.JOB_NAME,
        A.ITEM_CODE,
        A.REVISION,
        A.OPERATION_SEQ_NUM,
        A.DEPT_CODE,
        A.DEPT_NAME,
        A.PREFER_SITE_ID,
        A.PREFER_RESOURCE_ID,
        A.PREFER_RESOURCE,
        A.DESCRIPTION,
        A.USE_YN,
        A.INSERT_ID,
        A.INSERT_DTTM,
        A.UPDATE_ID,
        A.UPDATE_DTTM,
        I.CUSTOMER,
        AC.CUSTOMER_NAME AS CUSTOMER_NAME,
        I.MODEL_NAME   ,    
        DM.SITE_ID
 FROM   TH_TAR_LOT_ROUTE_PREFER_RESOURCE A
 LEFT OUTER JOIN CBST_SPEC_BASIC I ON  A.ITEM_CODE = I.ITEM_CODE  AND A.REVISION = I.REVISION  and A.ORGANIZATION_ID = I.ORGANIZATION_ID
 LEFT OUTER JOIN AR_CUSTOMERS AC ON I.CUSTOMER = AC.CUSTOMER_NUMBER
 LEFT OUTER JOIN TH_TAR_DEPT_MASTER DM  ON A.DEPT_CODE = DM.DEPARTMENT_CODE
WHERE   
    1=1
");
            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
        AND A.ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
            }

            if (terms["REVISION"].Length > 0)
            {
                sSQL.Append($@"
        AND A.REVISION = '{terms["REVISION"].AsString()}'
");
            }

            if (terms["LOTNO"].Length > 0)
            {
                sSQL.Append($@"
        AND A.JOB_NAME LIKE '%{terms["LOTNO"].AsString()}%'
");
            }



            //if (terms["group_id"].Length > 0)
            //{
            //    sSQL.Append($@"
            //        AND B.DIVISION_ID = '{terms["group_id"].AsString()}'
            //    ");
            //}
            ////if (terms["item_code"].Length > 0)
            ////{
            ////    sSQL.Append($@"
            ////        AND A.ITEM_CODE = '{terms["item_code"].AsString()}'
            ////    ");
            ////}
            //if (terms["LOTNO"].Length > 0)
            //{
            //
            //    .Append($@"
            //        AND A.JOB_NAME = '{terms["LOTNO"].AsString()}'
            //    ");
            //}
            sSQL.Append($@"
");



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

MERGE INTO [dbo].[TH_TAR_LOT_ROUTE_PREFER_RESOURCE] AS Target
USING (
    SELECT 
            {ITEM["ORGANIZATION_ID"].V} AS ORGANIZATION_ID,
            {ITEM["DIVISION_ID"].V} AS DIVISION_ID,
            {ITEM["JOB_ID"].V} AS JOB_ID,
            {ITEM["JOB_NAME"].V} AS JOB_NAME,
            {ITEM["ITEM_CODE"].V} AS ITEM_CODE,
            {ITEM["REVISION"].V} AS REVISION,
            {ITEM["OPERATION_SEQ_NUM"].V} AS OPERATION_SEQ_NUM,
            {ITEM["DEPT_CODE"].V} AS DEPT_CODE,
            {ITEM["DEPT_NAME"].V} AS DEPT_NAME,
            {ITEM["SITE_ID"].V} AS PREFER_SITE_ID,
            {ITEM["PREFER_RESOURCE_ID"].V} AS PREFER_RESOURCE_ID,
            {ITEM["PREFER_RESOURCE"].V} AS PREFER_RESOURCE,
            {ITEM["DESCRIPTION"].V} AS DESCRIPTION,
            {ITEM["USE_YN"].V} AS USE_YN,
            {ITEM["INSERT_ID"].V} AS INSERT_ID,            
            {ITEM["UPDATE_ID"].V} AS UPDATE_ID
) AS source
ON Target.ITEM_CODE = Source.ITEM_CODE
   AND Target.REVISION = Source.REVISION
   AND Target.DEPT_CODE = Source.DEPT_CODE  
   AND Target.JOB_ID = Source.JOB_ID 
   AND Target.OPERATION_SEQ_NUM = Source.OPERATION_SEQ_NUM  
   AND Target.PREFER_SITE_ID = Source.PREFER_SITE_ID  
WHEN MATCHED THEN
    UPDATE SET 
        Target.JOB_NAME = Source.JOB_NAME,
        Target.ITEM_CODE = Source.ITEM_CODE,
        Target.REVISION = Source.REVISION,
        Target.DEPT_NAME = Source.DEPT_NAME,
        Target.PREFER_SITE_ID = Source.PREFER_SITE_ID,
        Target.PREFER_RESOURCE_ID = Source.PREFER_RESOURCE_ID,
        Target.PREFER_RESOURCE = Source.PREFER_RESOURCE,
        Target.DESCRIPTION = Source.DESCRIPTION,        
        Target.UPDATE_ID = Source.UPDATE_ID,
        Target.UPDATE_DTTM = GETDATE() 
WHEN NOT MATCHED THEN
    INSERT (
        ORGANIZATION_ID, DIVISION_ID, JOB_ID, JOB_NAME, ITEM_CODE, REVISION,
        OPERATION_SEQ_NUM, DEPT_CODE, DEPT_NAME, PREFER_SITE_ID,
        PREFER_RESOURCE_ID, PREFER_RESOURCE, DESCRIPTION, USE_YN,
        INSERT_ID, INSERT_DTTM, UPDATE_ID, UPDATE_DTTM
    )
    VALUES (
        Source.ORGANIZATION_ID, Source.DIVISION_ID, Source.JOB_ID, Source.JOB_NAME, Source.ITEM_CODE, Source.REVISION,
        Source.OPERATION_SEQ_NUM, Source.DEPT_CODE, Source.DEPT_NAME, Source.PREFER_SITE_ID,
        Source.PREFER_RESOURCE_ID, Source.PREFER_RESOURCE, Source.DESCRIPTION, 'Y',
        Source.INSERT_ID, GETDATE() , Source.UPDATE_ID, null
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
              DELETE FROM TH_TAR_LOT_ROUTE_PREFER_RESOURCE        
               WHERE ORGANIZATION_ID = {ITEM["ORGANIZATION_ID"].V} 
                 AND DIVISION_ID = {ITEM["DIVISION_ID"].V} 
                 AND JOB_ID = {ITEM["JOB_ID"].V} 
                 AND ITEM_CODE = {ITEM["ITEM_CODE"].V} 
                 AND OPERATION_SEQ_NUM = {ITEM["OPERATION_SEQ_NUM"].V} 
                 AND DEPT_CODE = {ITEM["DEPT_CODE"].V} 
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
