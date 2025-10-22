using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using InfluxDB.Client.Api.Domain;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_MST_PLAN_OPTION
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
            // 뒤에 %붙은 데이터는 100으로 나눠서 저장
            Data["waitLeadTime"] = Data["waitLeadTime"].AsNum() / 100;
            Data["processLeadTime"] = Data["processLeadTime"].AsNum() / 100;
            Data["dayCapacityLimit"] = Data["dayCapacityLimit"].AsNum() / 100;

            sSQL.Append($@"

EXECUTE [dbo].[SP_SAVE_TH_MST_PLAN_OPTION]
    {Data["waitLeadTime"].V}
    , {Data["processLeadTime"].V}
    , {Data["dayCapacityLimit"].V}
    , {Data["MASS"].V}
    , {Data["SAMPLE"].V}
    , {Data["TEST"].V}
    , {Data["HOLD"].V}
    , {Data["JIG"].V}
    , {Data["MPS"].V}
    , {Data["INPUT_PLAN"].V}
    , {Data["material_constraint"].V}
    , {Data["USER_ID"].V}
");

            // 생산 Type 저장



            // 공정그룹 별 구분별 계획비율 %



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

            Console.WriteLine("++SAVE");
            Console.WriteLine(DataList);

            sSQL.Append(Proc(DataList));

            //            sSQL.Append(@"
            //SELECT @USER_ID AS USER_ID;
            //");
            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
