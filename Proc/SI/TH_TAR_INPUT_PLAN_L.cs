using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using InfluxDB.Client.Api.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_TAR_INPUT_PLAN_L
    {
        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static string Proc(Params Data, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            DTClient.UserInfoMerge(Data);



            sSQL.Append($@"
EXECUTE [dbo].[SP_SAVE_TH_TAR_INPUT_PLAN_L]
    '{version}'
    ,{Data["CUST_NAME"].V}
    ,{Data["CUSTOMER_NUMBER"].V}
    ,{Data["SHIP_TO"].V}
    ,{Data["END_CUST"].V}
    ,{Data["BOOKED_FLAG"].V}
    ,{Data["ITEM_CODE"].V}
    ,{Data["MODEL_NAME"].V}
    ,{Data["SO_NUMBER"].V}
    ,{Data["SCHEDULE_LINE_ID"].V}
    ,{Data["SCHEDULE_LINE_SEQ"].V}
    ,{Data["STD_LEAD_TIME"].V}
    ,{Data["ATP_LEAD_TIME"].V}
    ,{Data["APS_LEAD_TIME"].V}
    ,{Data["STD_LT_INPUT_DATE"].V}
    ,{Data["ATP_LT_INPUT_DATE"].V}
    ,{Data["APS_LT_INPUT_DATE"].V}
    ,{Data["NEW_DATE"].V}
    ,{Data["ORDER_DATE"].V}
    ,{Data["WAITING_DAYS"].V}
    ,{Data["READY_BY_INPUT_PNL_QTY"].V}
    ,{Data["READY_BY_INPUT_SQM_QTY"].V}
    ,{Data["READY_BY_INPUT_PCS_QTY"].V}
    ,{Data["APS_PLAN_INPUT_DATE"].V}
    ,{Data["INPUT_PNL_QTY"].V}
    ,{Data["SQM_PER_PNL_RATIO"].V}
    ,{Data["PCS_PER_PNL_RATIO"].V}
    ,{Data["NEW_START_DATE"].V}
    ,{Data["DESCRIPTION"].V}
    ,{Data["CCL_EXPIRATION_DATE"].V}
    ,{Data["CCL_EXPIRATION_DAYS"].V}
    ,{Data["PPG_EXPIRATION_DATE"].V}
    ,{Data["PPG_EXPIRATION_DAYS"].V}
    ,{Data["BBT_YN"].V}
    ,{Data["BBT_JIG_CAPA"].V}
    ,{Data["LOT_SIZE"].V}
    ,{Data["SHRINKAGE_RATE"].V}
    ,{Data["USER_ID"].V}
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
        public static string Proc(ParamList DataList, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            DataList.ForEach(ITEM =>
            {
                sSQL.Append(Proc(ITEM, version));
            });

            return sSQL.ToString();
        }

        /// <summary>
        /// 공통 저장
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static Params Save(Params Data, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(Proc(Data, version));

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
        public static Params Save(ParamList DataList, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(Proc(DataList, version));

//            sSQL.Append(@"
//SELECT @USER_ID AS USER_ID;
//");
            
            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
