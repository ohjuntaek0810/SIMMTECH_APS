using DocumentFormat.OpenXml.Drawing;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class item_by_lt_input : BasePageModel
    {
        public item_by_lt_input()
        {
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                toClient["data"] = this.Save(dataList);
            }

            else if (e.Command == "delete")
            {
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                toClient["data"] = this.delete(dataList);
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
select 
    * 
from  
    TH_TAR_OM_ITEM_LEADTIME with (nolock)
WHERE
    1=1
    AND ORGANIZATION_ID = 101
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
    AND DIVISION_ID = {terms["group_id"].V}
");
            }

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
    AND ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
            }

            sSQL.Append($@"
ORDER BY INSERT_DTTM DESC, ITEM_CODE
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private bool Save(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"  

DECLARE @DIVISION_ID nvarchar(50);
DECLARE @ITEM_CODE nvarchar(100);
DECLARE @ITEM_LT_DAY decimal;


            ");
            foreach (Params ITEM in dataList)
            {
                DTClient.UserInfoMerge(ITEM);
                sSQL.Append($@"  
SET @DIVISION_ID  = {ITEM["DIVISION_ID"].V};
SET @ITEM_CODE  = {ITEM["ITEM_CODE"].V};
SET @ITEM_LT_DAY  = {ITEM["ITEM_LT_DAY"].V};


IF EXISTS (
    SELECT 1 FROM TH_TAR_OM_ITEM_LEADTIME 
    WHERE DIVISION_ID = @DIVISION_ID AND ITEM_CODE = @ITEM_CODE 
)
BEGIN
    -- UPDATE 로직

      UPDATE TH_TAR_OM_ITEM_LEADTIME SET           
            ITEM_LT_DAY = @ITEM_LT_DAY,
            UPDATE_ID = '{Cookie<User>.Store.USER_ID}',
            UPDATE_DTTM = GETDATE()
       WHERE  DIVISION_ID = @DIVISION_ID AND ITEM_CODE = @ITEM_CODE;

END
ELSE
BEGIN
    INSERT INTO TH_TAR_OM_ITEM_LEADTIME (
        ORGANIZATION_ID,
        DIVISION_ID,
        ITEM_CODE,
        ITEM_LT_DAY,
        USE_YN,
        INSERT_ID,
        INSERT_DTTM
    ) VALUES (
        101,
        @DIVISION_ID,
        @ITEM_CODE,
        @ITEM_LT_DAY,
        'Y',
        '{Cookie<User>.Store.USER_ID}',
        GETDATE()
    )
END
");
            }

            Console.WriteLine(sSQL.ToString());

            try
            {
                HS.Web.Common.Data.Execute(sSQL.ToString());
                return true;
            } catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return false;
            }
            
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private bool delete(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();
            dataList.ForEach(ITEM =>
            {
                sSQL.Append($@"
                DELETE FROM TH_TAR_OM_ITEM_LEADTIME   
                 WHERE ITEM_CODE  = {ITEM["ITEM_CODE"].V} 
                   AND DIVISION_ID = {ITEM["DIVISION_ID"].V}
                ;");

            });

            Console.WriteLine(sSQL.ToString());

            try
            {
                HS.Web.Common.Data.Execute(sSQL.ToString());
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return false;
            }

        }





    }
}
