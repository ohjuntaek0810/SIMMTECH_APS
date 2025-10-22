using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class SI_GRID
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
            string USER_ID = Cookie<User>.Store.USER_ID;

            sSQL.Append($@"

EXECUTE [dbo].[SP_SAVE_TH_GUI_GRID]
    {USER_ID}
    , {Data["grid_id"].V}
    , {Data["label"].V}
    , {Data["column_order"].D}
    , {Data["visible"].V}
    , {Data["width"].D}
    , {Data["fixed"].V}
    , {Data["editable"].V}
    , {Data["dataField"].V}
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

//            sSQL.Append(@"
//SELECT @USER_ID AS USER_ID;
//");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
