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
    public class control_cust_resource : BasePageModel
    {
        public control_cust_resource()
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
                return toClient;
            }

            if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];
                
                this.Save(dataList);
                return toClient;
            }

            if (e.Command == "delete")
            {
                Params searchterms = e.Params["searchterms"];
                ParamList dataList = e.Params["data"];
                this.delete(dataList);
                toClient["data"] = this.Search(searchterms);
                return toClient;
            }

            if (e.Command == "GET_CBST_SPEC_BASIC")
            {
                Params trems = e.Params["trems"];
                if (trems.Count == 0)
                {
                    throw new Exception("조회조건이 없습니다");
                }
                toClient["trems_result"] = HS.Web.Common.ApsManage.GET_CBST_SPEC_BASIC(trems);
                return toClient;
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
                 A.ITEM_CUST_IDX
                ,A.ORGANIZATION_ID
                ,A.DIVISION_ID
                ,A.INPUT_TYPE
                ,A.ITEM_CODE
                ,A.ITEM_NAME
                ,A.CUSTOMER_NUMBER
                ,A.CUSTOMER_NAME
                ,A.RESOURCE_CAPA_GROUP_ID
                ,A.RESOURCE_CAPA_GROUP_NAME
                ,A.PREFER_SITE_ID
                ,A.PREFER_RESOURCE_ID
                ,A.PREFER_RESOURCE_NAME
                ,A.DESCRIPTION
                ,A.USE_YN
            FROM  TH_TAR_ITEM_CUST_PREFER_RESOURCE A 
            WHERE 1=1 
            ");

            // 사업부
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
                AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
            }

            // RESOURCE_CAPA_GROUP
            if (terms["resource_capa_group"].Length > 0)
            {
                sSQL.Append($@"
                AND A.RESOURCE_CAPA_GROUP_NAME = {terms["resource_capa_group"].V}
                ");
            }

            // use_yn
            if (terms["use_yn"].Length > 0)
            {
                sSQL.Append($@"
                     AND isnull(B.USE_YN, 'Y') =  {terms["use_yn"].V}
                ");
            }

            sSQL.Append($@"
            ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="dataList"></param>      
        private void Save(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();
           
            sSQL.Append($@"  
DECLARE @ITEM_CUST_IDX	bigint;
DECLARE @ORGANIZATION_ID	float;
DECLARE @DIVISION_ID	nvarchar(300);
DECLARE @INPUT_TYPE	nvarchar(300);
DECLARE @ITEM_CODE	nvarchar(300);
DECLARE @ITEM_NAME	nvarchar(300)
DECLARE @CUSTOMER_NUMBER	int;
DECLARE @CUSTOMER_NAME	nvarchar(300);
DECLARE @RESOURCE_CAPA_GROUP_ID	nvarchar(300);
DECLARE @RESOURCE_CAPA_GROUP_NAME	nvarchar(300);
DECLARE @PREFER_SITE_ID	nvarchar(300);
DECLARE @PREFER_RESOURCE_ID	nvarchar(300);
DECLARE @PREFER_RESOURCE_NAME	nvarchar(300);
DECLARE @DESCRIPTION	nvarchar(4000);
DECLARE @USE_YN	nvarchar(2);
");

            foreach (Params ITEM in dataList)
            {
                DTClient.UserInfoMerge(ITEM);
                string USER_ID = Cookie<User>.Store.USER_ID;
                sSQL.Append($@"
                SET @ITEM_CUST_IDX	            = {ITEM["ITEM_CUST_IDX"].D};
                SET @ORGANIZATION_ID	        = {ITEM["ORGANIZATION_ID"].V};
                SET @DIVISION_ID	            = {ITEM["DIVISION_ID"].V};
                SET @INPUT_TYPE	                = {ITEM["INPUT_TYPE"].V};
                SET @ITEM_CODE	                = {ITEM["ITEM_CODE"].V};
                SET @ITEM_NAME	                = {ITEM["ITEM_NAME"].V};
                SET @CUSTOMER_NUMBER	        = {ITEM["CUSTOMER_NUMBER"].V};
                SET @CUSTOMER_NAME	            = {ITEM["CUSTOMER_NAME"].V};
                SET @RESOURCE_CAPA_GROUP_ID	    = {ITEM["RESOURCE_CAPA_GROUP_ID"].V};
                SET @RESOURCE_CAPA_GROUP_NAME	= {ITEM["RESOURCE_CAPA_GROUP_NAME"].V};
                SET @PREFER_SITE_ID	            = {ITEM["PREFER_SITE_ID"].V};
                SET @PREFER_RESOURCE_ID	        = {ITEM["PREFER_RESOURCE_ID"].V};
                SET @PREFER_RESOURCE_NAME	    = {ITEM["PREFER_RESOURCE_NAME"].V};
                SET @DESCRIPTION	            = {ITEM["DESCRIPTION"].V};
                SET @USE_YN	                    = {ITEM["USE_YN"].V};

                MERGE INTO TH_TAR_ITEM_CUST_PREFER_RESOURCE AS TARGET
                USING (
                    SELECT 
                        @ITEM_CUST_IDX  AS ITEM_CUST_IDX,
                        @ORGANIZATION_ID AS ORGANIZATION_ID,
                        @DIVISION_ID AS DIVISION_ID,
                        @INPUT_TYPE AS INPUT_TYPE,
                        @ITEM_CODE AS ITEM_CODE,
                        @ITEM_NAME AS ITEM_NAME,
                        @CUSTOMER_NUMBER AS CUSTOMER_NUMBER,
                        @CUSTOMER_NAME AS CUSTOMER_NAME,
                        @RESOURCE_CAPA_GROUP_ID AS RESOURCE_CAPA_GROUP_ID,
                        @RESOURCE_CAPA_GROUP_NAME AS RESOURCE_CAPA_GROUP_NAME,
                        @PREFER_SITE_ID AS PREFER_SITE_ID,
                        @PREFER_RESOURCE_ID AS PREFER_RESOURCE_ID,
                        @PREFER_RESOURCE_NAME AS PREFER_RESOURCE_NAME,
                        @DESCRIPTION AS DESCRIPTION,
                        @USE_YN AS USE_YN,
                       '{USER_ID}' AS INSERT_ID,
                        GETDATE() AS INSERT_DTTM,
                       '{USER_ID}' AS UPDATE_ID,
                        GETDATE() AS UPDATE_DTTM
                ) AS SOURCE
                ON TARGET.ITEM_CUST_IDX = SOURCE.ITEM_CUST_IDX   
                WHEN MATCHED THEN

                    UPDATE SET
                        TARGET.ORGANIZATION_ID = SOURCE.ORGANIZATION_ID,
                        TARGET.DIVISION_ID = SOURCE.DIVISION_ID,
                        TARGET.INPUT_TYPE = SOURCE.INPUT_TYPE,
                        TARGET.ITEM_NAME = SOURCE.ITEM_NAME,
                        TARGET.CUSTOMER_NAME = SOURCE.CUSTOMER_NAME,
                        TARGET.RESOURCE_CAPA_GROUP_ID = SOURCE.RESOURCE_CAPA_GROUP_ID,
                        TARGET.RESOURCE_CAPA_GROUP_NAME = SOURCE.RESOURCE_CAPA_GROUP_NAME,
                        TARGET.PREFER_SITE_ID = SOURCE.PREFER_SITE_ID,
                        TARGET.PREFER_RESOURCE_NAME = SOURCE.PREFER_RESOURCE_NAME,
                        TARGET.DESCRIPTION = SOURCE.DESCRIPTION,
                        TARGET.USE_YN = SOURCE.USE_YN,
                        TARGET.UPDATE_ID = SOURCE.UPDATE_ID,
                        TARGET.UPDATE_DTTM = SOURCE.UPDATE_DTTM

                WHEN NOT MATCHED THEN
                    INSERT (
                        ITEM_CUST_IDX,
                        ORGANIZATION_ID,
                        DIVISION_ID,
                        INPUT_TYPE,
                        ITEM_CODE,
                        ITEM_NAME,
                        CUSTOMER_NUMBER,
                        CUSTOMER_NAME,
                        RESOURCE_CAPA_GROUP_ID,
                        RESOURCE_CAPA_GROUP_NAME,
                        PREFER_SITE_ID,
                        PREFER_RESOURCE_ID,
                        PREFER_RESOURCE_NAME,
                        DESCRIPTION,
                        USE_YN,
                        INSERT_ID,
                        INSERT_DTTM,
                        UPDATE_ID,
                        UPDATE_DTTM
                    )
                    VALUES (
                        (SELECT ISNULL(MAX(ITEM_CUST_IDX), 0) + 1 FROM TH_TAR_ITEM_CUST_PREFER_RESOURCE),
                        SOURCE.ORGANIZATION_ID,
                        SOURCE.DIVISION_ID,
                        SOURCE.INPUT_TYPE,
                        SOURCE.ITEM_CODE,
                        SOURCE.ITEM_NAME,
                        SOURCE.CUSTOMER_NUMBER,
                        SOURCE.CUSTOMER_NAME,
                        SOURCE.RESOURCE_CAPA_GROUP_ID,
                        SOURCE.RESOURCE_CAPA_GROUP_NAME,
                        SOURCE.PREFER_SITE_ID,
                        SOURCE.PREFER_RESOURCE_ID,
                        SOURCE.PREFER_RESOURCE_NAME,
                        SOURCE.DESCRIPTION,
                        'Y',
                        SOURCE.INSERT_ID,
                        SOURCE.INSERT_DTTM,
                        SOURCE.UPDATE_ID,
                        SOURCE.UPDATE_DTTM
                    );
                ");
            }



            HS.Web.Common.Data.Execute(sSQL.ToString());

        }

        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveDetail(ParamList dataList)
        {
            HS.Web.Proc.LOOKUP_VALUE_M.Save(dataList);
        }

        private void delete(ParamList data)
        {
            StringBuilder sSQL = new StringBuilder();
            data.ForEach(ITEM =>
            {
                sSQL.Append($@"
                DELETE FROM TH_TAR_ITEM_CUST_PREFER_RESOURCE   
                 WHERE ITEM_CUST_IDX  = {ITEM["ITEM_CUST_IDX"].V}        
                ;");              

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

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Lot_Routing_Sequence_" + timestamp);
            }
            else
                return Page();
        }
    }
}
