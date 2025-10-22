using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_GUI_MENU
    {
        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static string Proc(Params Data)
        {
            StringBuilder sSQL = new StringBuilder();

            DTClient.UserInfoMerge(Data);

            sSQL.Append($@"

DECLARE @MENU_CD VARCHAR(100)

SET @MENU_CD = {Data["MENU_CD"].V}

EXECUTE [dbo].[SP_SAVE_TH_GUI_MENU] 
   {Data["CLIENT"].V}	    -- @CMP_CD
  ,@MENU_CD	                -- @MENU_CD
  ,{Data["MENU_NM"].V}	    -- @MENU_NM
  ,{Data["UP_MENU_CD"].V}   -- @UP_MENU_CD
  ,{Data["SORT"].D}		    -- @SORT
  ,{Data["S_URL"].V}		-- @S_URL
  ,{Data["MENU_NM_DTL"].V}	-- @MENU_NM_DTL
  ,{Data["I_CLASS"].V}		-- @I_CLASS
  ,{Data["TARGET"].V}		-- @TARGET
  ,{Data["VIEW_YN"].V}		-- @VIEW_YN
  ,{Data["USE_YN"].V}		-- @USE_YN
  ,{Data["USER_ID"].V}	    -- @USER_ID
");

            return sSQL.ToString();
        }

        /// <summary>
        /// 공통 저장
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static Params Save(Params Data)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.AppendLine(Proc(Data));

            sSQL.AppendLine("SELECT @MENU_CD AS MENU_CD");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
