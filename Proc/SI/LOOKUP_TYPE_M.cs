using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class LOOKUP_TYPE_M
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

            var segment1 = Data["SEGMENT1"].V == "null" ? "'-'" : Data["SEGMENT1"].V;
            var segment2 = Data["SEGMENT2"].V == "null" ? "'-'" : Data["SEGMENT2"].V;
            var segment3 = Data["SEGMENT3"].V == "null" ? "'-'" : Data["SEGMENT3"].V;
            var segment4 = Data["SEGMENT4"].V == "null" ? "'-'" : Data["SEGMENT4"].V;
            var segment5 = Data["SEGMENT5"].V == "null" ? "'-'" : Data["SEGMENT5"].V;
            var segment6 = Data["SEGMENT6"].V == "null" ? "'-'" : Data["SEGMENT6"].V;
            var segment7 = Data["SEGMENT7"].V == "null" ? "'-'" : Data["SEGMENT7"].V;
            var segment8 = Data["SEGMENT8"].V == "null" ? "'-'" : Data["SEGMENT8"].V;

            sSQL.Append($@"

EXECUTE [dbo].[SP_SAVE_LOOKUP_TYPE_M]
    {Data["ORIGN_LOOKUP_TYPE_CODE"].V}
    , {Data["LOOKUP_TYPE_CODE"].V}
    , {Data["LOOKUP_TYPE_VERSION"].V}
    , {Data["ACTIVE_FLAG"].V}
    , {Data["IS_LATEST_VERSION_YN"].V}
    , {segment1}
    , {segment2}
    , {segment3}
    , {segment4}
    , {segment5}
    , {segment6}
    , {segment7}
    , {segment8}
    , {Data["ATTRIBUTE01"].V}
    , {Data["ATTRIBUTE02"].V}
    , {Data["ATTRIBUTE03"].V}
    , {Data["ATTRIBUTE04"].V}
    , {Data["ATTRIBUTE05"].V}
    , {Data["ATTRIBUTE06"].V}
    , {Data["ATTRIBUTE07"].V}
    , {Data["ATTRIBUTE08"].V}
    , {Data["ATTRIBUTE09"].V}
    , {Data["ATTRIBUTE10"].V}
    , {Data["ATTRIBUTE11"].V}
    , {Data["ATTRIBUTE12"].V}
    , {Data["ATTRIBUTE13"].V}
    , {Data["ATTRIBUTE14"].V}
    , {Data["ATTRIBUTE15"].V}
    , {Data["USER_ID"].V}
    , {Data["TYPE_DESCRIPTION"].V}
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

//            sSQL.Append(@"
//SELECT @USER_ID AS USER_ID;
//");
            
            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
