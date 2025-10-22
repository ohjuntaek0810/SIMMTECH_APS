//using GrapeCity.DataVisualization.TypeScript;
using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Data;
using System.Text;

namespace HS.Web.Proc
{
    public class BI_CST
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

            sSQL.AppendLine($@"
SET @CST_CD :=  {Data["CST_CD"].V};

CALL sp_BI_CST
(
    {Data["CLIENT"].V}
    , @CST_CD
    , {Data["CST_NM"].V}
    , {Data["CST_BIZ_NO"].V}
    , {Data["CST_CEO_NM"].V}
    , {Data["CST_TYP"].V}
    , {Data["CST_IND"].V}
    , {Data["CST_TEL"].V}
    , {Data["CST_FAX"].V}
    , {Data["CST_EML"].V}
    , {Data["CST_ADDR1"].V}
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
                sSQL.AppendLine(Proc(ITEM));
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

            sSQL.AppendLine(Proc(Data));

            sSQL.AppendLine("SELECT @CST_CD AS CST_CD;");

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

            sSQL.AppendLine(Proc(DataList));

            sSQL.AppendLine("SELECT @CST_CD AS CST_CD;");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }

        public static DataTable Search(Params Data)
        {
            DTClient.UserInfoMerge(Data);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT 
	 BI_CST.CMP_CD
    ,BI_CST.CST_CD
    ,BI_CST.CST_NM
    ,$GetBizNo(BI_CST.CST_BIZ_NO)  AS CST_BIZ_NO
    ,BI_CST.CST_CEO_NM
    ,BI_CST.CST_TYP
    ,BI_CST.CST_IND
    ,BI_CST.CST_TEL
    ,BI_CST.CST_FAX
    ,BI_CST.CST_EML
    ,BI_CST.CST_ADDR1
    ,BI_CST.REG_DM
    ,BI_CST.REG_ID
    ,REG_EMP.EMP_NM AS REG_NM
FROM BI_CST
LEFT JOIN TH_GUI_USER AS REG_USER ON REG_USER.CLIENT = BI_CST.CMP_CD AND REG_USER.USER_ID = BI_CST.REG_ID
LEFT JOIN BI_EMP AS REG_EMP ON REG_EMP.CMP_CD = BI_CST.CMP_CD AND REG_EMP.EMP_CD = REG_USER.EMP_CD
WHERE 1 = 1
    AND BI_CST.CMP_CD  = '{Data["CLIENT"].AsString()}'
");
            if (Data["CST_CD"].Length > 0)
            {
                sSQL.AppendLine($"    AND BI_CST.CST_CD = '{Data["CST_CD"].AsString()}'");
            }

            if (Data["CST_NM"].Length > 0)
            {
                sSQL.AppendLine($"    AND BI_CST.CST_NM LIKE '%{Data["CST_NM"].AsString()}%'");
            }

            sSQL.AppendLine("ORDER BY BI_CST.CST_CD");

            return HS.Web.Common.Data.Get(sSQL.ToString()).Tables[0];

        }


        public static bool ExistsCustomerCode(Params Data)
        {
            DTClient.UserInfoMerge(Data);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT 1
FROM BI_CST
WHERE 1 = 1
    AND CMP_CD  = '{Data["CLIENT"].AsString()}'
    AND CST_CD  = '{Data["CST_CD"].AsString()}'
");
            DataTable dt = HS.Web.Common.Data.Get(sSQL.ToString()).Tables[0];

            return (dt.Rows.Count > 0);

        }
    }
}
