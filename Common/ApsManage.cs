using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Text;

namespace HS.Web.Common
{
    public class ApsManage
    {
        /// <summary>
        /// 즐겨찾기 확인
        /// </summary>
        public static DataTable searchPlanId()
        {

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
	PLAN_ID, 
	PLAN_ATTB_1 as WIP_YYYYMMDD,  
	PLAN_ATTB_2 as WIP_SEQ
from  
	th_mst_plan with (nolock)
where
	1=1
	AND PLAN_START_DTTM > (SELECT DATEADD(""WEEK"", -4, GETDATE()))
	and IS_FINISHED = 'Y'
order by INSERT_DTTM desc
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 즐겨찾기 추가
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void addFavorite(Params data)
        {
            //string CLIENT = Cookie<User>.Store.CLIENT;
            string USER_ID = Cookie<User>.Store.USER_ID;
            string CLIENT = "0100";

            string MENU_ID = data["curMenuId"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
INSERT INTO TH_GUI_USER_FAVORITES (USER_ID, MENU_ID)
VALUES ('{USER_ID}', '{MENU_ID}')
");
            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        /// <summary>
        /// 즐겨찾기 해제
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void deleteFavorite(Params data)
        {
            //string CLIENT = Cookie<User>.Store.CLIENT;
            string USER_ID = Cookie<User>.Store.USER_ID;
            string CLIENT = "0100";

            string MENU_ID = data["curMenuId"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
DELETE FROM TH_GUI_USER_FAVORITES
WHERE USER_ID = '{USER_ID}' AND MENU_ID = '{MENU_ID}';
");
            HS.Web.Common.Data.Execute(sSQL.ToString());
        }


        // ========================================================================================================
        // GRID 개인화 관련
        /// <summary>
        /// 저장된 그리드 헤더컬럼 가져오기
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static DataTable SearchGrid(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            string USER_ID = Cookie<User>.Store.USER_ID;
            string GRID_ID = terms["grid_id"].AsString();

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
	SUG.DATA_FIELD AS dataField,
    SUG.COLUMN_NAME AS label,
	SUG.VISIBLE AS visible,
	SUG.WIDTH AS width,
	SUG.FIX AS fixed,
	SUG.EDITABLE AS editable
FROM
	TH_GUI_USER_GRID SUG
WHERE
	1=1
	AND SUG.USER_ID = '{USER_ID}'
	AND SUG.GRID_ID = '{GRID_ID}'
ORDER BY SUG.COLUMN_ORDER
");

            return Data.Get(sSQL.ToString()).Tables[0];

        }

        /// <summary>
        /// 그리드 헤더컬럼 옵션 저장
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void SaveGrid(ParamList dataList)
        {
            HS.Web.Proc.SI_GRID.Save(dataList);
        }

        public static DataTable EXEC_PR_OM_DAILY_CAPA_CALC(Params Terms)
        {
           
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"     
                    DECLARE @P_MASTER_ID VARCHAR(150) ;
                    DECLARE @P_PLAN_ID VARCHAR(150) ;
                    SET @P_MASTER_ID  = 'SIMMTECH';
                    SET @P_PLAN_ID = {Terms["PLAN_ID"].V};
                    EXEC PR_OM_DAILY_CAPA_CALC @P_MASTER_ID, @P_PLAN_ID;
                    SELECT 'OK'AS RTN ;
                    ");
            DataTable dt = Data.Get(sSQL.ToString()).Tables[0];
            return dt; 
        }



        public static DataTable GET_CBST_SPEC_BASIC(Params Terms)
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
                   WHERE ITEM_CODE= {Terms["ITEM_CODE"].V}
                )
                SELECT 
                    A.ITEM_CODE,
                    A.REVISION,
                    A.MODEL_REV,
                    A.CUSTOMER,
                    A.D_CATEGORY,
                    A.MODEL_NAME,
                    A.MODEL_NAME AS ITEM_NAME,
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



    }
}
