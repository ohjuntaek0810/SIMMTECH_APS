using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using InfluxDB.Client.Api.Domain;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_TAR_READY_BY_INPUT_L
    {
        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static string Proc(Params item, string division, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            DTClient.UserInfoMerge(item);

            var new_start_date = "";
            if (item["NEW_START_DATE"].AsString() != "")
            {
                new_start_date = Convert.ToDateTime(item["NEW_START_DATE"].AsString()).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                new_start_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            var new_date = "";
            if (item["NEW_DATE"].AsString() != "")
            {
                new_date = Convert.ToDateTime(item["NEW_DATE"].AsString()).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                new_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            string PROD_GROUP = "";
            if(division == "SPS")
            {
                PROD_GROUP = "S";
            } else if (division == "HDI")
            {
                PROD_GROUP = "H";
            }


            sSQL.Append($@"

EXECUTE [dbo].[SP_SAVE_TH_TAR_READY_BY_INPUT_L]
    '{version}',
    '{PROD_GROUP}',
    {item["RELEASE_STATUS"].V},
    {item["PRIMARY_DESC"].V},
    {item["ALTERNATE_DESC"].V},
    {item["CCL_QTY"].V},
    {item["FLM"].V},
    {item["MFG_CATEGORY"].V},
    {item["LAYER"].V},
    {item["CUST_NAME"].V},
    {item["LAYUP_DESC"].V},
    {item["SHRINKAGE_RATE"].V},
    {item["ITEM_CODE"].V},
    {item["MODEL_NAME"].V},
    {item["LEAD_TIME"].V},
    '{new_start_date}',
    '{new_date}',
    {item["QTY_PCS"].V},
    {item["PNL_QTY"].V},
    {item["M2_QTY"].V},
    {item["COMPONENT_ITEM_CODE"].V},
    {item["COMPONENT_ITEM_DESC"].V},
    {item["ST1001"].V},
    {item["ST1003"].V},
    {item["ST1008"].V},
    {item["ST1010"].V},
    {item["IDF_HDI"].V},
    {item["MLB_HDI"].V},
    {item["DRILL_HDI"].V},
    {item["LASER_DRILL_HDI"].V},
    {item["CU_PLATING_HDI"].V},
    {item["DES_DEB_HDI"].V},
    {item["VIA_FILL_HDI"].V},
    {item["ODF_HDI"].V},
    {item["HOLE_PLUGGING_HDI"].V},
    {item["SM_SPRAY_HDI"].V},
    {item["SM_SCREEN_HDI"].V},
    {item["MARKING_HDI"].V},
    {item["AU_MASKING_HDI"].V},
    {item["STRIPPING_HDI"].V},
    {item["HARD_AU_HDI"].V},
    {item["ROUTER_HDI"].V},
    {item["CHAMFER_HDI"].V},
    {item["BBT_HDI"].V},
    {item["OSP_HDI"].V},
    {item["AFVI_HDI"].V},
    {item["MLB"].V},
    {item["HALF_ETCHING"].V},
    {item["LASER_DRILL"].V},
    {item["ELESS"].V},
    {item["TENTING"].V},
    {item["VIAFILL"].V},
    {item["HOLEPLUG"].V},
    {item["DFSR_FLAT"].V},
    {item["DFSR"].V},
    {item["FLAT"].V},
    {item["HARDAU"].V},
    {item["ENEPIG"].V},
    {item["SLOTPUNCH"].V},
    {item["BBT"].V},
    {item["SB"].V},
    {item["SOP"].V},
    {item["SOFTAU"].V},
    {item["SM"].V},
    {item["ITS"].V},
    {item["LDI_IL"].V},
    {item["LDI_OL"].V},
    {item["LDI_LM"].V},
    {item["DI_SM"].V},
    {item["PF_IL"].V},
    {item["PF_OL"].V},
    {item["PNF_IL"].V},
    {item["PNF_OL"].V},
    {item["OSPT"].V},
    {item["OSPS"].V},
    {item["AU_MASK1"].V},
    {item["AU_MASK2"].V},
    {item["AU_PLASMA"].V},
    {item["F53ECU"].V},
    {item["F53VECU"].V},
    {item["F33ECU"].V},
    {item["F52ECU"].V},
    {item["F33SMCZ"].V},
    {item["F52SMCZ"].V},
    {item["FS1SMCZ"].V},
    {item["BARCODE_CO"].V},
    {item["BARCODE_YAG"].V},
    {item["DFR"].V},
    {item["S_DESMEAR"].V},
    {item["UV_BARCODE"].V},
    {item["DES_OL"].V},
    {item["F42_CU"].V},
    {item["F93_CU"].V},
    {item["F9_DESIL"].V},
    {item["F9_DESOL"].V}
");

            return sSQL.ToString();
        }

        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="DataList"></param>
        /// <returns></returns>
        public static string Proc(ParamList DataList, string division, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            DataList.ForEach(ITEM =>
            {
                sSQL.Append(Proc(ITEM, division, version));
            });

            return sSQL.ToString();
        }

        /// <summary>
        /// 공통 저장
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static Params Save(Params Data, string division, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(Proc(Data, division, version));

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
        public static Params Save(ParamList DataList, string division, string version)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(Proc(DataList, division, version));

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
