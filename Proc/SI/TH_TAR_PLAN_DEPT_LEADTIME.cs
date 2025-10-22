using HS.Core;
using HS.Web.Common;
using HS.Web.Logic;
using InfluxDB.Client.Api.Domain;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Proc
{
    public class TH_TAR_PLAN_DEPT_LEADTIME
    {
        /// <summary>
        /// 공통 저장프로시저
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static string Proc(Params Data)
        {
         //   @DIVISION_ID NVARCHAR(50)
	        //,@DEPARTMENT_CLASS_CODE NVARCHAR(10)
	        //,@DEPARTMENT_CODE NVARCHAR(10)   --여기까지 Primary Key Check
	        //,@PLAN_PROCESSING_TIME_HR FLOAT(8)
	        //,@PLAN_ESSENTIAL_WAITING_TIME_HR FLOAT(8)
	        //,@PLAN_OPER_WAITING_TIME_HR FLOAT(8)
	        //,@USER_ID NVARCHAR(50) 
            StringBuilder sSQL = new StringBuilder();

            DTClient.UserInfoMerge(Data);

            sSQL.Append($@"

EXECUTE [dbo].[SP_SAVE_TH_TAR_PLAN_DEPT_LEADTIME]
    {Data["GROUP"].V}
    , {Data["CLASS CODE"].V}
    , {Data["DEPARTMENT CODE"].V}
    , {Data["PROCESS(PLNA L/T)"].D}
    , {Data["READY(PLAN L/T)"].D}
    , {Data["WAIT(PLAN L/T)"].D}
    , {Data["USER_ID"].V}
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
