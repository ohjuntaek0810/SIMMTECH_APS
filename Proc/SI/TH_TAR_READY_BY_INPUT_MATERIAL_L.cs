using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using InfluxDB.Client.Api.Domain;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_TAR_READY_BY_INPUT_MATERIAL_L
    {
        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static string Proc(Params item)
        {
            StringBuilder sSQL = new StringBuilder();

            DTClient.UserInfoMerge(item);

            var new_start_date = "";
            if (item["NEW_START_DATE"].AsString() != "")
            {
                new_start_date = Convert.ToDateTime(item["NEW_START_DATE"].AsString()).ToString("yyyy-MM-dd HH:mm:ss");
            }
            var new_date = "";
            if (item["NEW_DATE"].AsString() != "")
            {
                new_date = Convert.ToDateTime(item["NEW_DATE"].AsString()).ToString("yyyy-MM-dd HH:mm:ss");
            }

            sSQL.Append($@"

EXECUTE [dbo].[SP_SAVE_TH_TAR_READY_BY_INPUT_MATERIAL_L]
    {item["GUBUN"].V},
    {item["PRODUCT_GROUP_CODE"].V},
    {item["ASSEMBLY_ITEM_CODE"].V},
    {item["CUSTOMER_NAME"].V},
    {item["MODEL_NAME"].V},
    '{new_start_date}',
    '{new_date}',
    {item["ASSY_QTY"].V},
    {item["ASSY_M2"].V},
    {item["COMPONET_ITEM_CODE"].V},
    {item["COMPONENT_ITEM"].V},
    {item["COMPONENT_QUANTITY"].V},
    {item["EXTENDED_QUANTITY"].V},
    {item["COMP_ITEM_TYPE"].V},
    {item["UOM"].V},
    {item["LAYER"].V},
    {item["CATEGORY_NAME"].V},
    {item["MFG_DATE"].V},
    {item["EXPIRATION_DATE"].V},
    {item["EXPIRATION_DAY"].V}
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

            Console.WriteLine("DataList Length = ");
            Console.WriteLine(DataList.Count);

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
