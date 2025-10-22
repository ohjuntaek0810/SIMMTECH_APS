using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Spreadsheet;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class delivery_request : BasePageModel
    {
        public delivery_request()
        {
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params searchterms = e.Params["searchterms"];
                toClient["MasterData"] = this.SearchMaster(searchterms);
                return toClient;
            }

            if (e.Command == "search_detail")
            {
                Params rowterms = e.Params["rowterms"];      
                toClient["DetailData"] = this.SearchDetail(rowterms);
                return toClient;
            }

            if (e.Command == "saveMaster")
            {
                Params searchterms = e.Params["searchterms"];
                ParamList dataList = e.Params["data"];
                ValiList vali = new ValiList(dataList);
                vali.Null("MODEL_NAME", "Item 정보는 필수 입력 사항입니다.");
                vali.Null("REVISION", "Item 정보는 필수 입력 사항입니다.");
                vali.Null("ORDER_DATE", "ORDER_DATE 가 입력되지 않았습니다.");
                vali.Null("DIVISION_ID", "DIVISION_ID 가 입력되지 않았습니다.");
                vali.DoneDeco();
                this.SaveMaster(dataList);
                toClient["MasterData"] = this.SearchMaster(searchterms);
                return toClient;                
            }

            if (e.Command == "save_sortMaster")
            {
                Params searchterms = e.Params["searchterms"];
                ParamList dataList = e.Params["data"];
                ValiList vali = new ValiList(dataList);
                vali.Null("MODEL_NAME", "Item 정보는 필수 입력 사항입니다.");
                vali.Null("REVISION", "Item 정보는 필수 입력 사항입니다.");
                vali.Null("ORDER_DATE", "ORDER_DATE 가 입력되지 않았습니다.");
                vali.Null("DIVISION_ID", "DIVISION_ID 가 입력되지 않았습니다.");
                vali.DoneDeco();
                this.save_sortMaster(dataList);
                toClient["MasterData"] = this.SearchMaster(searchterms);
                return toClient;
            }


            if (e.Command == "delete")
            {
                Params searchterms = e.Params["searchterms"];
                ParamList dataList = e.Params["data"];
                this.delete(dataList);
                toClient["MasterData"] = this.SearchMaster(searchterms);
                return toClient;
            }
            if (e.Command == "saveDetail")
            {
                Params searchterms = e.Params["searchterms"];
                Params Masterterms = e.Params["Masterterms"];
                ParamList dataList = e.Params["data"];
                ValiList vali = new ValiList(dataList);
                vali.Null("REQUEST_ORDER_ID", "REQUEST_ORDER_ID가 입력되지 않았습니다.");             
                vali.Null("ORDER_DATE", "ORDER_DATE 가 입력되지 않았습니다.");
                vali.Null("DIVISION_ID", "DIVISION_ID 가 입력되지 않았습니다.");
                vali.Null("ITEM_CODE", "ITEM 정보가 입력되지 않았습니다.");               
                vali.DoneDeco();

                this.SaveDetail(dataList);
                toClient["DetailData"] = this.SearchDetail(Masterterms);
                return toClient;
            }

            if (e.Command == "save_sortDetail")
            {
                Params searchterms = e.Params["searchterms"];
                Params Masterterms = e.Params["Masterterms"];
                ParamList dataList = e.Params["data"];

                ValiList vali = new ValiList(dataList);
                vali.Null("REQUEST_ORDER_ID", "REQUEST_ORDER_ID가 입력되지 않았습니다.");
                vali.Null("ORDER_DATE", "ORDER_DATE 가 입력되지 않았습니다.");
                vali.Null("DIVISION_ID", "DIVISION_ID 가 입력되지 않았습니다.");
                vali.Null("ITEM_CODE", "ITEM 정보가 입력되지 않았습니다.");
                vali.DoneDeco();
                this.save_sortDetail(dataList);
                toClient["DetailData"] = this.SearchDetail(Masterterms);
                return toClient;
            }

            if (e.Command == "deleteDetail")
            {
                Params searchterms = e.Params["searchterms"];
                Params Masterterms = e.Params["Masterterms"];
                ParamList dataList = e.Params["data"];
                this.deleteDetail(dataList);
                toClient["DetailData"] = this.SearchDetail(Masterterms);
            }
            if (e.Command == "promising_split")
            {
                Params searchterms = e.Params["searchterms"];
                Params pnl_promising_terms = e.Params["pnl_promising_terms"];

                this.PROMISING_SPLIT(pnl_promising_terms);

                toClient["DetailData"] = this.SearchDetail(pnl_promising_terms);

                return toClient;
            }
            if (e.Command == "EXEC_PR_OM_MASTER")
            {
               
                StringBuilder sSQL = new StringBuilder();
                //EXEC PR_OM_MASTER;
                sSQL.Append($@"                    
                    EXEC PR_OM_MASTER;
                    SELECT 'OK'AS RTN ;
                    ");
                DataTable dt = Data.Get(sSQL.ToString()).Tables[0];

                toClient["RESULT_PARAMS"] = dt.ToParams();
                return toClient;
            }

            if (e.Command == "GET_CBST_SPEC_BASIC")
            {
                Params trems = e.Params["trems"];
                if(trems.Count == 0)
                {
                    throw new Exception("조회조건이 없습니다");
                }
                toClient["trems_result"] = GET_CBST_SPEC_BASIC(trems);

            }

            if (e.Command == "GET_EXCEL_INPUT_DATA")
            {
                ParamList TermsList = e.Params["TermsList"];
                if (TermsList.Count == 0)
                {
                    throw new Exception("조회조건이 없습니다");
                }
                toClient["TermsList_result"] = GET_EXCEL_INPUT_DATA(TermsList);
            }


            if (e.Command == "search_jig")
            {
                Params trems = e.Params["data"];
                toClient["jig_result"] = this.SEARCH_JIG(trems);
            }
            if (e.Command == "SEARCH_LATEST_DUE_DATE")
            {
                Params trems = e.Params["data"];
                toClient["DUE_DATE_RESULT"] = this.SEARCH_LATEST_DUE_DATE(trems);
            }



            


            return toClient;
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchMaster(Params terms)
        {
            DTClient.UserInfoMerge(terms);          

            var sSQL = new StringBuilder();

            sSQL.Append($@"
               SELECT M.*              
                FROM TH_TAR_OM_ORDER_REQUEST M
                WHERE 
                    M.DIVISION_ID = {terms["group_id"].V}
                AND M.ORDER_DATE BETWEEN {terms["search_order_date"].V.Replace("-", "")} AND  {terms["search_order_date"].V.Replace("-", "")}
              ");

            sSQL.Append($@"
              ORDER BY M.ORDER_DATE   ,    REQUEST_ORDER_SORT_ORDER          
              ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchDetail(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            var sSQL = new StringBuilder();
            sSQL.Append($@"
               SELECT 
                    D.DIVISION_ID ,
                    D.ORDER_DATE,
                    D.REQUEST_ORDER_ID,
                    D.REQUEST_ORDER_DUE_SEQ,
                    D.REQUEST_ORDER_DUE_SEQ_ORDER,
                    D.TOTAL_SORT_ORDER,
                    D.ITEM_CODE,
                    D.MODEL_NAME,
                    D.REVISION,
                    D.PROMISING_DATE,
                    D.QTY_PCS,
                    D.DESCRIPTION
                FROM TH_TAR_OM_ORDER_PROMISING D            
               WHERE
                    D.DIVISION_ID= {terms["DIVISION_ID"].V}
                AND D.ORDER_DATE= {terms["ORDER_DATE"].V}
                AND D.REQUEST_ORDER_ID= {terms["REQUEST_ORDER_ID"].V}
              ");           
            sSQL.Append($@"
              ORDER BY D.DIVISION_ID , D.ORDER_DATE,D.REQUEST_ORDER_ID ,  D.REQUEST_ORDER_DUE_SEQ_ORDER          
              ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }


        private void SaveMaster(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();
           
            sSQL.Append($@"  

DECLARE @ORDER_DATE nvarchar(16) ;
DECLARE @DIVISION_ID nvarchar(20) ;
DECLARE @REQUEST_ORDER_ID nvarchar(300) ;
DECLARE @REQUEST_ORDER_SORT_ORDER int ;
DECLARE @ITEM_CODE nvarchar(510) ;
DECLARE @MODEL_NAME NVARCHAR(2000) ;
DECLARE @REVISION nvarchar(6) ;
DECLARE @TOTAL_QTY_PCS float ;
DECLARE @DESCRIPTION nvarchar(20);
DECLARE @TEAM nvarchar(400) ;
DECLARE @CUST_ID float ;
DECLARE @CUST_NAME nvarchar(2000) ;
DECLARE @SHIP_TO_ID float ;
DECLARE @SHIP_TO_NAME nvarchar(2000) ;
DECLARE @PO_NUMBER nvarchar(300) ;
DECLARE @CUST_PO_NUMBER nvarchar(300) ;
DECLARE @SCHEDULE_SHIP_DATE nvarchar(20) ;
DECLARE @REQUEST_DATE NVARCHAR(20) ;
DECLARE @PROMISE_DATE NVARCHAR(20);
DECLARE @SHIP_DATE NVARCHAR(20) ;
DECLARE @INSERT_ID NVARCHAR(100) ;
DECLARE @INSERT_DTTM datetime ;
DECLARE @UPDATE_ID NVARCHAR(150) ;
DECLARE @UPDATE_DTTM datetime ;


            ");
            foreach (Params ITEM in dataList)
            {
                DTClient.UserInfoMerge(ITEM);
                sSQL.Append($@"  
SET @ORDER_DATE  = {ITEM["ORDER_DATE"].V.Replace("-","")};
SET @DIVISION_ID  = {ITEM["DIVISION_ID"].V};
SET @REQUEST_ORDER_ID = {ITEM["REQUEST_ORDER_ID"].V};
SET @REQUEST_ORDER_SORT_ORDER  =  {ITEM["REQUEST_ORDER_SORT_ORDER"].V};
SET @ITEM_CODE = {ITEM["ITEM_CODE"].V.Trim()};
SET @MODEL_NAME  = {ITEM["MODEL_NAME"].V};
SET @REVISION  =  {ITEM["REVISION"].V};
SET @TOTAL_QTY_PCS  = {ITEM["TOTAL_QTY_PCS"].D.Replace(",","").Trim()};
SET @DESCRIPTION  = {ITEM["DESCRIPTION"].V};
SET @TEAM  = {ITEM["TEAM"].V.Trim()};
SET @CUST_ID  = {ITEM["CUST_ID"].V};
SET @CUST_NAME  = {ITEM["CUST_NAME"].V};
SET @SHIP_TO_ID  = {ITEM["SHIP_TO_ID"].V};
SET @SHIP_TO_NAME = {ITEM["SHIP_TO_NAME"].V};
SET @PO_NUMBER  = {ITEM["PO_NUMBER"].V};
SET @CUST_PO_NUMBER  = {ITEM["CUST_PO_NUMBER"].V};
SET @SCHEDULE_SHIP_DATE  = {ITEM["SCHEDULE_SHIP_DATE"].V};
SET @REQUEST_DATE  = {ITEM["REQUEST_DATE"].V};
SET @PROMISE_DATE  = {ITEM["PROMISE_DATE"].V};
SET @SHIP_DATE  = {ITEM["SHIP_DATE"].V};
SET @INSERT_ID  = {ITEM["USER_ID"].V};
SET @INSERT_DTTM  = {ITEM["INSERT_DTTM"].V};
SET @UPDATE_ID  = {ITEM["USER_ID"].V};
SET @UPDATE_DTTM  = {ITEM["UPDATE_DTTM"].V};


IF EXISTS (
    SELECT 1 FROM TH_TAR_OM_ORDER_REQUEST WHERE DIVISION_ID = @DIVISION_ID AND ORDER_DATE = @ORDER_DATE AND  REQUEST_ORDER_ID = @REQUEST_ORDER_ID
)
BEGIN
    -- UPDATE 로직

      UPDATE TH_TAR_OM_ORDER_REQUEST SET           
            REQUEST_ORDER_SORT_ORDER = @REQUEST_ORDER_SORT_ORDER,
            ITEM_CODE = @ITEM_CODE,
            MODEL_NAME = @MODEL_NAME,
            REVISION = @REVISION,
            TOTAL_QTY_PCS = @TOTAL_QTY_PCS,
            DESCRIPTION = @DESCRIPTION,
            TEAM = @TEAM,
            CUST_ID = @CUST_ID,
            CUST_NAME = @CUST_NAME,
            SHIP_TO_ID = @SHIP_TO_ID,
            SHIP_TO_NAME = @SHIP_TO_NAME,
            PO_NUMBER = @PO_NUMBER,
            CUST_PO_NUMBER = @CUST_PO_NUMBER,
            SCHEDULE_SHIP_DATE = @SCHEDULE_SHIP_DATE,
            REQUEST_DATE = @REQUEST_DATE,
            PROMISE_DATE = @PROMISE_DATE,
            SHIP_DATE = @SHIP_DATE,
            UPDATE_ID = @UPDATE_ID,
            UPDATE_DTTM = @UPDATE_DTTM
       WHERE 
             DIVISION_ID = @DIVISION_ID 
         AND ORDER_DATE = @ORDER_DATE 
         AND REQUEST_ORDER_ID = @REQUEST_ORDER_ID ;

END
ELSE
BEGIN
        -- INSERT 로직

        SELECT @REQUEST_ORDER_ID = 
            ISNULL(MAX(REQUEST_ORDER_ID), 0) + 1
        FROM TH_TAR_OM_ORDER_REQUEST
        WHERE 
            DIVISION_ID = @DIVISION_ID
            AND ORDER_DATE = @ORDER_DATE;


        SELECT @REQUEST_ORDER_SORT_ORDER = 
            ISNULL(MAX(REQUEST_ORDER_SORT_ORDER), 0) + 1
        FROM TH_TAR_OM_ORDER_REQUEST
        WHERE 
            DIVISION_ID = @DIVISION_ID
            AND ORDER_DATE = @ORDER_DATE;



        INSERT INTO TH_TAR_OM_ORDER_REQUEST(
            ORDER_DATE,
            DIVISION_ID,
            REQUEST_ORDER_ID,
            REQUEST_ORDER_SORT_ORDER,
            ITEM_CODE,
            MODEL_NAME,
            REVISION,
            TOTAL_QTY_PCS,
            DESCRIPTION,
            TEAM,
            CUST_ID,
            CUST_NAME,
            SHIP_TO_ID,
            SHIP_TO_NAME,
            PO_NUMBER,
            CUST_PO_NUMBER,
            SCHEDULE_SHIP_DATE,
            REQUEST_DATE,
            PROMISE_DATE,
            SHIP_DATE,
            INSERT_ID,
            INSERT_DTTM,
            UPDATE_ID,
            UPDATE_DTTM
        )
        VALUES (
            @ORDER_DATE,
            @DIVISION_ID,
            @REQUEST_ORDER_ID,
            @REQUEST_ORDER_SORT_ORDER,
            @ITEM_CODE,
            @MODEL_NAME,
            @REVISION,
            @TOTAL_QTY_PCS,
            @DESCRIPTION,
            @TEAM,
            @CUST_ID,
            @CUST_NAME,
            @SHIP_TO_ID,
            @SHIP_TO_NAME,
            @PO_NUMBER,
            @CUST_PO_NUMBER,
            @SCHEDULE_SHIP_DATE,
            @REQUEST_DATE,
            @PROMISE_DATE,
            @SHIP_DATE,
            @INSERT_ID,
            @INSERT_DTTM,
            @UPDATE_ID,
            @UPDATE_DTTM
        );
END;

");
            }



            HS.Web.Common.Data.Execute(sSQL.ToString());

        }


        private void save_sortMaster(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();

            foreach (Params ITEM in dataList)
            {
                sSQL.Append($@"  
               UPDATE TH_TAR_OM_ORDER_REQUEST SET   
                      REQUEST_ORDER_SORT_ORDER =  {ITEM["_key"].AsNum()+1}
                WHERE DIVISION_ID = {ITEM["DIVISION_ID"].V}
                  AND ORDER_DATE = {ITEM["ORDER_DATE"].V}
                  AND REQUEST_ORDER_ID = {ITEM["REQUEST_ORDER_ID"].V}
                ;");
            }
            HS.Web.Common.Data.Execute(sSQL.ToString());
        }       



        private void delete(ParamList data)
        {
            StringBuilder sSQL = new StringBuilder();
            data.ForEach(ITEM =>
            {
                sSQL.Append($@"
                DELETE FROM TH_TAR_OM_ORDER_REQUEST   
                 WHERE ORDER_DATE  = {ITEM["ORDER_DATE"].V} 
                   AND DIVISION_ID = {ITEM["DIVISION_ID"].V} 
                   AND REQUEST_ORDER_ID = {ITEM["REQUEST_ORDER_ID"].V} 
                ;");
                sSQL.Append($@"
                DELETE FROM TH_TAR_OM_ORDER_PROMISING        
                 WHERE ORDER_DATE  = {ITEM["ORDER_DATE"].V} 
                   AND DIVISION_ID = {ITEM["DIVISION_ID"].V} 
                   AND REQUEST_ORDER_ID = {ITEM["REQUEST_ORDER_ID"].V}               
                ;");

            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }


        private void SaveDetail(ParamList detailList)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"

                DECLARE @ORDER_DATE nvarchar(16) ;
                DECLARE @DIVISION_ID nvarchar(20) ;
                DECLARE @REQUEST_ORDER_ID int ;
                DECLARE @REQUEST_ORDER_DUE_SEQ int ;
                DECLARE @REQUEST_ORDER_DUE_SEQ_ORDER int ;
                DECLARE @TOTAL_SORT_ORDER int ;
                DECLARE @ITEM_CODE nvarchar(510) ;
                DECLARE @MODEL_NAME nvarchar(2000);
                DECLARE @REVISION nvarchar(6) ;
                DECLARE @PROMISING_DATE nvarchar(20) ;
                DECLARE @QTY_PCS float ;
                DECLARE @DESCRIPTION nvarchar(2000) ;
                DECLARE @INSERT_ID nvarchar(100) ;
                DECLARE @INSERT_DTTM datetime ;
                DECLARE @UPDATE_ID nvarchar(100) ;
                DECLARE @UPDATE_DTTM datetime ;

            ");
            foreach (Params ITEM in detailList)
            {
                DTClient.UserInfoMerge(ITEM);
                sSQL.Append($@"

 SET @ORDER_DATE  = {ITEM["ORDER_DATE"].V};
 SET @DIVISION_ID  = {ITEM["DIVISION_ID"].V};
 SET @REQUEST_ORDER_ID  = {ITEM["REQUEST_ORDER_ID"].V};
 SET @REQUEST_ORDER_DUE_SEQ = {ITEM["REQUEST_ORDER_DUE_SEQ"].V};
 SET @REQUEST_ORDER_DUE_SEQ_ORDER  = {ITEM["REQUEST_ORDER_DUE_SEQ_ORDER"].V};
 SET @TOTAL_SORT_ORDER  = {ITEM["TOTAL_SORT_ORDER"].V};
 SET @ITEM_CODE = {ITEM["ITEM_CODE"].V};
 SET @MODEL_NAME  = {ITEM["MODEL_NAME"].V};
 SET @REVISION  = {ITEM["REVISION"].V};
 SET @PROMISING_DATE  = {ITEM["PROMISING_DATE"].V};
 SET @QTY_PCS  = {ITEM["QTY_PCS"].V};
 SET @DESCRIPTION  = {ITEM["DESCRIPTION"].V};
 SET @INSERT_ID  = {ITEM["USER_ID"].V};
 SET @INSERT_DTTM = {ITEM["INSERT_DTTM"].V};
 SET @UPDATE_ID  = {ITEM["USER_ID"].V};
 SET @UPDATE_DTTM = {ITEM["UPDATE_DTTM"].V};


 
IF @PROMISING_DATE IS NOT NULL
BEGIN
    SET @PROMISING_DATE = CONVERT(VARCHAR(10), @PROMISING_DATE, 120) ;
END


IF EXISTS (
    SELECT 1 FROM TH_TAR_OM_ORDER_PROMISING 
    WHERE 
          DIVISION_ID = @DIVISION_ID
      AND ORDER_DATE = @ORDER_DATE
      AND REQUEST_ORDER_ID = @REQUEST_ORDER_ID 
      AND REQUEST_ORDER_DUE_SEQ = @REQUEST_ORDER_DUE_SEQ
)
BEGIN
    UPDATE TH_TAR_OM_ORDER_PROMISING SET   
        TOTAL_SORT_ORDER = @TOTAL_SORT_ORDER,
        ITEM_CODE = @ITEM_CODE,
        MODEL_NAME = @MODEL_NAME,
        REVISION = @REVISION,
        PROMISING_DATE = @PROMISING_DATE,
        QTY_PCS = @QTY_PCS,
        DESCRIPTION = @DESCRIPTION,
        UPDATE_ID = @UPDATE_ID,
        UPDATE_DTTM = @UPDATE_DTTM
     WHERE 
          DIVISION_ID = @DIVISION_ID
      AND ORDER_DATE = @ORDER_DATE
      AND REQUEST_ORDER_ID = @REQUEST_ORDER_ID 
      AND REQUEST_ORDER_DUE_SEQ = @REQUEST_ORDER_DUE_SEQ
END
ELSE
BEGIN


    -- 자동 증가 로직
    SELECT @REQUEST_ORDER_DUE_SEQ = 
        ISNULL(MAX(REQUEST_ORDER_DUE_SEQ), 0) + 1
    FROM TH_TAR_OM_ORDER_PROMISING
    WHERE ORDER_DATE = @ORDER_DATE
      AND DIVISION_ID = @DIVISION_ID
      AND REQUEST_ORDER_ID = @REQUEST_ORDER_ID;


    INSERT INTO TH_TAR_OM_ORDER_PROMISING (
        ORDER_DATE,
        DIVISION_ID,
        REQUEST_ORDER_ID,
        REQUEST_ORDER_DUE_SEQ,
        REQUEST_ORDER_DUE_SEQ_ORDER,
        TOTAL_SORT_ORDER,
        ITEM_CODE,
        MODEL_NAME,
        REVISION,
        PROMISING_DATE,
        QTY_PCS,
        DESCRIPTION,
        INSERT_ID,
        INSERT_DTTM,
        UPDATE_ID,
        UPDATE_DTTM
    )
    VALUES (
        @ORDER_DATE,
        @DIVISION_ID,
        @REQUEST_ORDER_ID,
        @REQUEST_ORDER_DUE_SEQ,
        @REQUEST_ORDER_DUE_SEQ_ORDER,
        @TOTAL_SORT_ORDER,
        @ITEM_CODE,
        @MODEL_NAME,
        @REVISION,
        @PROMISING_DATE,
        @QTY_PCS,
        @DESCRIPTION,
        @INSERT_ID,
        @INSERT_DTTM,
        @UPDATE_ID,
        @UPDATE_DTTM
    );
END;

");
            }

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }


        private void save_sortDetail(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();

            foreach (Params ITEM in dataList)
            {
                sSQL.Append($@"  
               UPDATE TH_TAR_OM_ORDER_PROMISING SET   
                      REQUEST_ORDER_DUE_SEQ_ORDER =  {ITEM["_key"].AsNum() + 1}
                WHERE DIVISION_ID = {ITEM["DIVISION_ID"].V}
                  AND ORDER_DATE = {ITEM["ORDER_DATE"].V}
                  AND REQUEST_ORDER_ID = {ITEM["REQUEST_ORDER_ID"].V}
                  AND REQUEST_ORDER_DUE_SEQ = {ITEM["REQUEST_ORDER_DUE_SEQ"].V}
                ;");
            }
            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        private void deleteDetail(ParamList data)
        {
            StringBuilder sSQL = new StringBuilder();
            data.ForEach(ITEM =>
            {
                sSQL.Append($@"
                  DELETE FROM TH_TAR_OM_ORDER_PROMISING        
                   WHERE ORDER_DATE  = {ITEM["ORDER_DATE"].V} 
                     AND DIVISION_ID = {ITEM["DIVISION_ID"].V} 
                     AND REQUEST_ORDER_ID = {ITEM["REQUEST_ORDER_ID"].V} 
                     AND REQUEST_ORDER_DUE_SEQ = {ITEM["REQUEST_ORDER_DUE_SEQ"].V}                                    
                ;");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        private DataTable GET_CBST_SPEC_BASIC(Params terms)
        {
            var sSQL = new StringBuilder();
            sSQL.Append($@"

                -- MAX 리비전만 표시 
                WITH RankedItems AS (
                    SELECT 
                        ITEM_CODE,
                        REVISION,
                        MODEL_REV,
                        CUSTOMER,
                        D_CATEGORY,
                        MODEL_NAME,
                        ROW_NUMBER() OVER (PARTITION BY ITEM_CODE ORDER BY REVISION DESC) AS rn
                    FROM CBST_SPEC_BASIC
                   WHERE ITEM_CODE= {terms["ITEM_CODE"].V}
                )
                SELECT 
                    A.ITEM_CODE,
                    A.REVISION,
                    A.MODEL_REV,
                    A.CUSTOMER,
                    A.D_CATEGORY,
                    A.MODEL_NAME,
                    C.CUSTOMER_NAME AS CUSTOMER_NAME,
                    A.CUSTOMER AS CUST_ID,
                    C.CUSTOMER_NAME AS CUST_NAME
                FROM RankedItems A 
                LEFT OUTER JOIN AR_CUSTOMERS C ON A.CUSTOMER =  C.CUSTOMER_NUMBER
                WHERE A.rn = 1
;               
              ");            
            return Data.Get(sSQL.ToString()).Tables[0];
        }


        private DataTable GET_EXCEL_INPUT_DATA(ParamList terms)
        {
            
            var sSQL = new StringBuilder();

            var itemCodes = new List<string>();
            foreach (Params D in terms)
            {
                itemCodes.Add($"{D["ITEM_CODE"].V}");
            }
            var itemCodeCondition = string.Join(",", itemCodes);

            sSQL.Append($@"
                    SELECT
                        A.ITEM_CODE,
                        A.REVISION,
                        A.MODEL_REV,
                        A.CUSTOMER,
                        A.D_CATEGORY,
                        A.MODEL_NAME,
                        C.CUSTOMER_NAME AS CUSTOMER_NAME,
                        A.CUSTOMER AS CUST_ID,
                        C.CUSTOMER_NAME AS CUST_NAME
                    FROM (
                        SELECT 
                            ITEM_CODE,
                            REVISION,
                            MODEL_REV,
                            CUSTOMER,
                            D_CATEGORY,
                            MODEL_NAME,
                            ROW_NUMBER() OVER (PARTITION BY ITEM_CODE ORDER BY REVISION DESC) AS rn
                        FROM CBST_SPEC_BASIC
                       WHERE ITEM_CODE in ({itemCodeCondition})
                    ) A
                    LEFT OUTER JOIN AR_CUSTOMERS C ON A.CUSTOMER = C.CUSTOMER_NUMBER
                    WHERE A.rn = 1;            
              ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private void PROMISING_SPLIT(Params Terms)
        {
            StringBuilder sSQL = new StringBuilder();

            DTClient.UserInfoMerge(Terms);
            int totalQty = int.Parse(Terms["PROMISING_ORDER_QTY"].Value.ToString());
            int splitQty = int.Parse(Terms["PROMISING_SPLIT_QTY"].Value.ToString());
           

            string itemCode = Terms["PROMISING_ITEM_CODE"].Value.ToString();
            string orderDate = Terms["ORDER_DATE"].Value.ToString();
            string divisionId = Terms["DIVISION_ID"].Value.ToString();
            int requestOrderId = int.Parse(Terms["REQUEST_ORDER_ID"].Value.ToString());

            DateTime startDate = DateTime.Parse(Terms["PROMISING_MAKE_START_DATE"].Value.ToString());

     


            int fullSplits = totalQty / splitQty; //배분
            int remainder = totalQty % splitQty; //배분후 나머지 수량
            int totalRows = fullSplits + (remainder > 0 ? 1 : 0); //총행의 갯수

            for (int i = 0; i < fullSplits; i++)
            {
                DateTime promisingDate = startDate.AddDays(i); // +1일씩 증가
                sSQL.Append($@"
                    INSERT INTO TH_TAR_OM_ORDER_PROMISING (
                        ORDER_DATE,
                        DIVISION_ID,
                        REQUEST_ORDER_ID,
                        REQUEST_ORDER_DUE_SEQ,
                        REQUEST_ORDER_DUE_SEQ_ORDER,
                        TOTAL_SORT_ORDER,
                        ITEM_CODE,
                        MODEL_NAME,
                        REVISION,
                        PROMISING_DATE,
                        QTY_PCS,
                        INSERT_ID,
                        INSERT_DTTM
                    )
                    SELECT
                        '{orderDate}',
                        '{divisionId}',
                        {requestOrderId},
                        ISNULL(MAX(REQUEST_ORDER_DUE_SEQ), 0) + 1,  -- 증가 키
                        ISNULL(MAX(REQUEST_ORDER_DUE_SEQ_ORDER), 0) + 1,  -- 증가 키
                        ISNULL(MAX(TOTAL_SORT_ORDER), 0) + 1,  -- 증가 키
                        '{itemCode}',
                        '{Terms["MODEL_NAME"].AsString()}',
                        '{Terms["REVISION"].AsString()}',
                        '{promisingDate:yyyy-MM-dd}',
                        {splitQty},
                        '{Terms["USER_ID"].AsString()}', 
                        GETDATE()
                    FROM TH_TAR_OM_ORDER_PROMISING
                    WHERE ORDER_DATE = '{orderDate}' AND DIVISION_ID = '{divisionId}' AND REQUEST_ORDER_ID = {requestOrderId};
                ");

            }


            //나머지 값 마지막 행에 넣기 
            if (remainder > 0)
            {
                DateTime promisingDate = startDate.AddDays(fullSplits); // 마지막 날짜
                sSQL.Append($@"
                    INSERT INTO TH_TAR_OM_ORDER_PROMISING (
                        ORDER_DATE,
                        DIVISION_ID,
                        REQUEST_ORDER_ID,
                        REQUEST_ORDER_DUE_SEQ,
                        REQUEST_ORDER_DUE_SEQ_ORDER,
                        TOTAL_SORT_ORDER,
                        ITEM_CODE,
                        MODEL_NAME,
                        REVISION,
                        PROMISING_DATE,
                        QTY_PCS,
                        INSERT_ID,
                        INSERT_DTTM
                    )
                    SELECT
                        '{orderDate}',
                        '{divisionId}',
                        {requestOrderId},
                        ISNULL(MAX(REQUEST_ORDER_DUE_SEQ), 0) + 1,  -- 증가 키
                        ISNULL(MAX(REQUEST_ORDER_DUE_SEQ_ORDER), 0) + 1,  -- 증가 키
                        ISNULL(MAX(TOTAL_SORT_ORDER), 0) + 1,  -- 증가 키
                        '{itemCode}',
                        '{Terms["MODEL_NAME"].AsString()}',
                        '{Terms["REVISION"].AsString()}',
                        '{promisingDate:yyyy-MM-dd}',
                        {remainder},
                        '{Terms["USER_ID"].AsString()}', 
                        GETDATE()
                    FROM TH_TAR_OM_ORDER_PROMISING
                    WHERE ORDER_DATE = '{orderDate}' AND DIVISION_ID = '{divisionId}' AND REQUEST_ORDER_ID = {requestOrderId};
                ");
            }



              
            HS.Web.Common.Data.Execute(sSQL.ToString());

        }


        private DataTable SEARCH_JIG(Params data)
        {
            var sSQL = new StringBuilder();
            sSQL.Append($@"
               SELECT FLOOR(TOT_JIG_CAPA_PCS_with_YIELD) AS BBT_CAPA
                FROM TH_TAR_JIG_CAPA 
                WHERE ITEM_CODE = {data["ITEM_CODE"].V};
");
            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SEARCH_LATEST_DUE_DATE(Params data)
        {
            var sSQL = new StringBuilder();
            sSQL.Append($@" SELECT CONVERT(VARCHAR, dbo.GET_OM_LAST_OPO_DUE_DATE({data["ITEM_CODE"].V}), 120) AS DUE_DATE;   ");
            return Data.Get(sSQL.ToString()).Tables[0];
        }



    }
}

