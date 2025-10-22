using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_GUI_GRP
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

DECLARE @GRP_ID VARCHAR(20)

SET @GRP_ID = {Data["GRP_ID"].V}

EXECUTE [dbo].[SP_SAVE_TH_GUI_GRP] 
   {Data["CLIENT"].V}	    -- @CMP_CD
  ,@GRP_ID	                -- @GRP_ID
  ,{Data["GRP_NM"].V}	    -- @GRP_NM
  ,{Data["RMK"].V}          -- @RMK
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

            sSQL.AppendLine("SELECT @GRP_ID AS GRP_ID");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
