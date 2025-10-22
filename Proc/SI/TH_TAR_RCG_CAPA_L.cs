using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using InfluxDB.Client.Api.Domain;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_TAR_RCG_CAPA_L
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

            // SP_SAVE_TH_TAR_RCG_CAPA_L
            //@DIVISION_ID                                  NVARCHAR(200)
            //,@RESOURCE_CAPA_GROUP_ID                      NVARCHAR(200)
            //,@RESOURCE_CAPA_GROUP_NAME                    NVARCHAR(200)
            //,@APS_RESOURCE_ID                             NVARCHAR(200)
            //,@APS_RESOURCE_NAME                           NVARCHAR(200)
            //,@USER_TOTAL_SITE_RCG_CAPA_M2_DAY_29          FLOAT
            //,@SITE_ID                                     NVARCHAR(200)
            //,@OWN_OUT_GBN                                 NVARCHAR(200)
            //,@RESOURCE_LEVEL                              NVARCHAR(200)
            //,@LEVEL2_GBN                                  NVARCHAR(200)
            //,@TOTAL_SITE_M2_MONTH_PROD                    FLOAT
            //,@TOTAL_SITE_RCG_CAPA_M2_DAY_29               FLOAT
            //,@RES_CNT                                     INT
            //,@AVG_SITE_RCG_CAPA_M2_DAY_29                 FLOAT
            //,@USE_YN                                      NVARCHAR(1)
            //,@USER_ID                                     NVARCHAR(20)

            sSQL.Append($@"
EXECUTE [dbo].[SP_SAVE_TH_TAR_RCG_CAPA_L]
    {Data["GROUP"].V}
    , {Data["CAPA GROUP ID"].V}
    , {Data["CAPA GROUP NAME"].V}
    , {Data["RESOURCE CAPA ID"].V}
    , {Data["RESOURCE CAPA NAME"].V}
    , {Data["USE CAPA/DAY"].D}
    , {Data["SITE"].V}
    , {Data["OWN_OUT_GBN"].V}
    , {Data["RESOURCE_LEVEL"].V}
    , {Data["LEVEL2_GBN"].V}
    , {Data["CAPA/MONTH"].D}
    , {Data["CAPA/DAY"].D}
    , {Data["RES_CNT"].D}
    , {Data["AVG_SITE_RCG_CAPA_M2_DAY_29"].D}
    , {Data["USE(Y/N)"].V}
    , {Data["USER_ID"].V}
");
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
                Console.WriteLine(ITEM);
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
