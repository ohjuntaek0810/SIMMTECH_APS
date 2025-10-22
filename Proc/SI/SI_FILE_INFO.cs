using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class SI_FILE_INFO
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

            // GUID 생성 
            string FILE_GUID = Data["FILE_GUID"].AsString();

            if (string.IsNullOrEmpty(FILE_GUID))
                FILE_GUID = Guid.NewGuid().ToString();

            sSQL.Append($@"
SET @FILE_GUID :=  '{FILE_GUID}';		

CALL sp_SI_FILE_INFO
(
    {Data["CLIENT"].V}
    , @FILE_GUID
    , {Data["FILE_GRP_NO"].D}
    , {Data["FILE_ORG_NAME"].V}
    , {Data["MDUL_CODE"].V}
    , {Data["FILE_PATH"].V}
    , {Data["FILE_MIME_TYPE"].V}
    , {Data["FILE_EXT"].V}
    , {Data["FILE_SIZE"].D}
    , {Data["USE_YN"].V}
    , {Data["USERID"].V}
);
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
SELECT @FILE_GUID AS FILE_GUID;
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
SELECT @FILE_GUID AS FILE_GUID;
");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }
}
