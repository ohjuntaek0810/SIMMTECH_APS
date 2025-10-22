//using GrapeCity.DataVisualization.TypeScript;
using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using MySqlX.XDevAPI;
using System.Data;
using System.Text;

namespace HS.Web.Proc
{
    public class SI_CODE_GROUP
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
DECLARE @GRP_CD VARCHAR(100)

SET @GRP_CD = {Data["GRP_CD"].V}

EXECUTE [dbo].[SP_SAVE_SI_CODE_GROUP] 
   {Data["CLIENT"].V}	    -- @CMP_CD
  ,@GRP_CD
  ,{Data["GRP_NM"].V}
  ,{Data["UP_GRP_CD"].V}
  ,{Data["USE_YN"].V}
  ,{Data["SYS_YN"].V}
  ,{Data["RMK"].V}
  ,{Data["USER_ID"].V}	    -- @USER_ID
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
SELECT @GRP_CD AS GRP_CD;
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
SELECT @GRP_CD AS GRP_CD;
");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }
    }

    public class SI_CODE_INFO
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
DECLARE @GRP_CD VARCHAR(20)
DECLARE @CMN_CD VARCHAR(20)

SET @GRP_CD = {Data["GRP_CD"].V}
SET @CMN_CD = {Data["CMN_CD"].V}

EXECUTE [dbo].[SP_SAVE_SI_CODE_INFO] 
   {Data["CLIENT"].V}	    -- @CMP_CD
  ,@GRP_CD
  ,@CMN_CD
  ,{Data["CMN_NM"].V}
  ,{Data["UP_CMN_CD"].V}
  ,{Data["SEQ"].D}
  ,{Data["USE_YN"].V}
  ,{Data["RMK"].V}
  ,{Data["USER_ID"].V}	    -- @USER_ID
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
SELECT 
     @GRP_CD AS GRP_CD
    ,@CMN_CD AS CMN_CD;
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
SELECT 
    @GRP_CD AS GRP_CD
    ,@CMN_CD AS CMN_CD;
");

            return HS.Web.Common.Data.Result(sSQL.ToString());
        }

        /// <summary>
        /// 삭제
        /// </summary>
        /// <param name="DataList"></param>
        /// <returns></returns>
        public static void Delete(string cmp_cd, string grp_cd, ParamList DataList)
        {
            StringBuilder sSQL = new StringBuilder();

            foreach (Params p in DataList)
            {
                sSQL.AppendLine($"DELETE FROM SI_CODE_INFO WHERE CMP_CD = '{cmp_cd}' AND GRP_CD = '{grp_cd}' AND CMN_CD = {p["CMN_CD"].V};");
            }

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }
    }
}
