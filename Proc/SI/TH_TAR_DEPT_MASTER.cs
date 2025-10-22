using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using InfluxDB.Client.Api.Domain;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_TAR_DEPT_MASTER
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

EXECUTE [dbo].[SP_SAVE_TH_TAR_DEPT_MASTER]
    {Data["GROUP"].V}
    , {Data["CLASS CODE"].V}
    , {Data["DEPARTMENT CODE"].V}
    , {Data["SITE"].V}
    , {Data["ROUTING NAME"].V}
    , {Data["APS_WIP_ROUTE_GRP_ID"].V}
    , {Data["APS_DEPT_GRP_ID"].V}
    , {Data["RESOURCE_CAPA_GROUP_ID"].V}
    , {Data["OUTSOURCE(Y/N)"].V}
    , {Data["OWN_OUT_SELECT_YN"].V}
    , {Data["USE(Y/N)"].V}
    , {Data["USER_ID"].V}
");
            //Console.WriteLine("Generated SQL:");
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
