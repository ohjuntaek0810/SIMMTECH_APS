using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_GUI_GRP_AUTH
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

EXECUTE [dbo].[SP_SAVE_TH_GUI_GRP_AUTH] 
   {Data["CLIENT"].V}	    -- @CMP_CD
  ,{Data["MENU_CD"].V}      -- @MENU_CD
  ,{Data["GRP_ID"].V}       -- @GRP_ID
  ,{Data["GRP_AUTH"].D}	    -- @GRP_AUTH
  ,{Data["USER_ID"].V}	    -- @USER_ID
");

            return sSQL.ToString();
        }

        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="DataList"></param>
        /// <returns></returns>
        public static string Proc(ParamList DataList)
        {
            StringBuilder sSQL = new StringBuilder();

            DataList.ForEach(ITEM =>
            {
                sSQL.Append(Proc(ITEM));
            });

            return sSQL.ToString();
        }

        /// <summary>
        /// 공통 저장
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static Params Save(ParamList DataList)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.AppendLine(Proc(DataList));

            //sSQL.AppendLine("SELECT @GRP_ID AS GRP_ID");

            return HS.Web.Common.Data.Result(sSQL.ToString());
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

            //sSQL.AppendLine("SELECT @GRP_ID AS GRP_ID");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
