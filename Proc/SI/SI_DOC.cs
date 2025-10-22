//using GrapeCity.DataVisualization.TypeScript;
using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Data;
using System.Text;

namespace HS.Web.Proc
{
    public class SI_DOC
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
SET @DOC_VER :=  {Data["DOC_VER"].D};

CALL sp_SI_DOC
(
    {Data["CLIENT"].V}
    , {Data["DOC_CLS"].V}
    , @DOC_VER
    , {Data["TITLE"].V}
    , {Data["FILE_GUID"].V}
    , {Data["FILE_NM"].V}
    , {Data["SYS_YN"].V}
    , {Data["RMK"].V}
    , {Data["USER_ID"].V}
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
SELECT @DOC_VER AS DOC_VER;
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
SELECT @DOC_VER AS DOC_VER;
");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }

        public static DataTable Search(Params Data)
        {
            DTClient.UserInfoMerge(Data);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT 
	 CMP_CD
    ,DOC_CLS
    ,DOC_VER
    ,TITLE
    ,FILE_GUID
    ,FILE_NM
    ,USE_YN
    ,RMK
    ,REG_DM
    ,REG_ID
    ,MDF_DM
    ,MDF_ID
FROM SI_DOC
WHERE 1 = 1
    AND CMP_CD  = '{Data["CLIENT"].AsString()}'
	AND DOC_CLS = '{Data["DOC_CLS"].AsString()}'
");
            if (Data["DOC_VER"].Length > 0)
            {
                sSQL.AppendLine($"    AND DOC_VER = {Data["DOC_VER"].AsNum()}");
            }

            if (Data["TITLE"].Length > 0)
            {
                sSQL.AppendLine($"    AND TITLE LIKE '%{Data["TITLE"].AsString()}%'");
            }

            sSQL.AppendLine("ORDER BY DOC_VER DESC");

            return HS.Web.Common.Data.Get(sSQL.ToString()).Tables[0];

        }

        public static decimal GetLastDocVersion(string doc_cls)
        {
            Params terms = new Params();
            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            decimal result = 0;

            sSQL.Append($@"
SELECT DOC_VER
FROM SI_DOC
WHERE 1 = 1
    AND CMP_CD  = '{terms["CLIENT"].AsString()}'
	AND DOC_CLS = '{doc_cls}'
ORDER BY DOC_VER
LIMIT 1");
            DataTable dt = HS.Web.Common.Data.Get(sSQL.ToString()).Tables[0];

            if (dt.Rows.Count > 0) result = Convert.ToDecimal(dt.Rows[0]["DOC_VER"]);

            return result;
        }

        public static string GetLastDocVersionFileGuid(string doc_cls)
        {
            Params terms = new Params();
            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            string result = "";

            sSQL.Append($"SELECT FILE_GUID FROM SI_DOC WHERE CMP_CD = '{terms["CLIENT"].AsString()}' AND DOC_CLS = '{doc_cls}' ORDER BY DOC_VER DESC LIMIT 1");
            
            DataTable dt = HS.Web.Common.Data.Get(sSQL.ToString()).Tables[0];

            if (dt.Rows.Count > 0) result = dt.Rows[0]["FILE_GUID"].ToString();

            return result;
        }
    }
}
