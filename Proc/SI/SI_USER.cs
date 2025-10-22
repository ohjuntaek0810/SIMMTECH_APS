using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_GUI_USER
    {
        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static string Proc(Params Data)
        {
            StringBuilder sSQL = new StringBuilder();

            // TODO : 로그인 구현 후 변경필요
            // string REG_ID = Cookie<User>.Store.USER_ID;
            string REG_ID = "admin";

            DTClient.UserInfoMerge(Data);

            sSQL.Append($@"
DECLARE @USER_ID VARCHAR(20)

SET @USER_ID = {Data["USER_ID"].V}

EXECUTE [dbo].[SP_SAVE_TH_GUI_USER]
    {Data["CLIENT"].V}
    , @USER_ID
    , {Data["USER_NM"].V}
    , {Data["PASSWD"].V}
    , {Data["LOGIN_GRP_CD"].V}
    , {Data["RMK"].V}
    , {Data["USE_YN"].V}
    , {REG_ID}
");
            Console.WriteLine("Generated SQL:");
            Console.WriteLine(sSQL.ToString());

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
        public static Params Save(Params Data)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(Proc(Data));

            sSQL.Append(@"
SELECT @USER_ID AS USER_ID;
");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }

        /// <summary>
        /// 공통 저장
        /// </summary>
        /// <param name="DataList"></param>
        /// <returns></returns>
        public static Params Save(ParamList DataList)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(Proc(DataList));

            sSQL.Append(@"
SELECT @USER_ID AS USER_ID;
");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
