using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class control_board : BasePageModel
    {

        public control_board()
        {
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
                toClient["data_search_count"] = this.SearchCount(terms);
                toClient["data_option"] = this.SearchOption(terms);
                toClient["data_dept_by"] = this.SearchOptionDeptBy(terms);
            }

            else if (e.Command == "search_option")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchOption(terms);
                toClient["data_dept_by"] = this.SearchOptionDeptBy(terms);
            }

            else if (e.Command == "create_version")
            {
                try
                {
                    this.create_version();
                } catch (Exception ex)
                {
                    toClient["data"] = ex.StackTrace;
                }

                toClient["data"] = "success";
            }

            else if (e.Command == "run")
            {
                try
                {
                    Params terms = e.Params["terms"];

                    this.run(terms);
                }
                catch (Exception ex)
                {
                    toClient["data"] = ex.StackTrace;
                }

                toClient["data"] = "success";
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                this.Save(dataList);
            }

            else if (e.Command == "save_option")
            {
                ParamList dataList = e.Params["data"];
                ParamList dataList_dept_by = e.Params["data_dept_by"];


                Console.WriteLine(dataList);

                // 데이터 저장
                this.SaveOption(dataList);
                this.SaveOptionDeptBy(dataList_dept_by);

            }

            else if (e.Command == "search_his")
            {
                Params terms = e.Params["terms"];

                toClient["data_search_his"] = this.Search_His(terms);
                toClient["data_search_his_dept_by"] = this.Search_His_DEPT_BY(terms); 
            }

            return toClient;
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable Search(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            sSQL.Append(@"
SELECT  A.MASTER_ID
      , A.PLAN_ID
      , A.INSERT_ID
      , A.DESCR
      , CASE WHEN A.IS_RUNNING = 'Y'  and B.OUTBOUND_FLAG = 'C' and A.IS_FINISHED is null and datediff(second, B.OUTBOUND_END, getdate()) > 60  THEN  'ERROR' --## PR_BAT_APS에서 1분마다 엔진 종료를 체크하기 전에 ERROR로 표시되는 것을 수정
        WHEN A.IS_RUNNING = 'N' and A.IS_FINISHED = 'E' then 'ERROR'  --##  2025-10-15 추가. 수동으로 에러 처리 시, IS_RUNNING = 'N',  IS_FINISHED = 'E'로 업데이트 할 것. 
        WHEN B.INBOUND_FLAG = 'R'  THEN  'CREATED'
        WHEN B.OUTBOUND_FLAG = 'C' THEN 'DONE'
        ELSE 'PLANING'
        END STATUS
      , B.INBOUND_START
      , B.INBOUND_END
      , B.ENGINE_START
      , B.ENGINE_END
      , B.OUTBOUND_START
      , B.OUTBOUND_END
  -- 참조용 
  --, A.IS_RUNNING
  --, A.IS_FINISHED 
FROM TH_MST_PLAN  A 
left outer join
TH_MST_ENG_PLAN_FLAG B 
on A.plan_id = B.plan_id 
WHERE A.MASTER_ID = 'SIMMTECH'
");
            if (terms["plan_date"].Length > 0)
            {
                sSQL.Append($@"
    AND   A.PLAN_ID   LIKE '%{terms["plan_date"].AsString().Replace("-", "")}%'
");
            }


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchCount(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            sSQL.Append(@"
-- 수립중인 계획이 있는지 확인하는 쿼리 --> 결과가 나오면 실행중인게 있음. 
SELECT  A.MASTER_ID
      , A.PLAN_ID
	  , A.IS_RUNNING
	  , A.IS_FINISHED
      , b.OUTBOUND_FLAG 
	  , b.INBOUND_FLAG 
	  , b.ENGINE_FLAG
	  , A.INSERT_ID
      , A.INSERT_DTTM
      , A.DESCR
      , CASE WHEN A.IS_RUNNING = 'Y' and A.IS_FINISHED is null and datediff(second, B.OUTBOUND_END, getdate()) > 60  THEN  'ERROR' --## 2025-09-30 수정. PR_BAT_APS에서 1분마다 엔진 종료를 체크하기 전에 ERROR로 표시되는 것을 수정 
             WHEN A.IS_RUNNING = 'N' and A.IS_FINISHED = 'E' then 'ERROR'  --##  2025-10-15 추가. 수동으로 에러 처리 시, IS_RUNNING = 'N',  IS_FINISHED = 'E'로 업데이트 할 것. 
			 WHEN B.INBOUND_FLAG = 'R'  THEN  'CREATED'
             WHEN B.OUTBOUND_FLAG = 'C' THEN 'DONE'
             ELSE 'PLANING'
        END STATUS
      , B.INBOUND_START
      , B.INBOUND_END
      , B.ENGINE_START
      , B.ENGINE_END
      , B.OUTBOUND_START
      , B.OUTBOUND_END
  -- 참조용 
  --, A.IS_RUNNING
  --, A.IS_FINISHED 
FROM TH_MST_PLAN  A 
	inner join
	TH_MST_ENG_PLAN_FLAG B 
	on A.plan_id = B.plan_id 
WHERE A.MASTER_ID = 'SIMMTECH'
--AND   A.PLAN_ID   LIKE '%20251014%'  -- 현재 수행중인 Plan 유무는 전체 기간 조회 해야 함 
--and   CONVERT(date, B.INSERT_DTTM) = CONVERT(date, GETDATE())  -- 당일자 생성된 것만 대상으로 제한하는 부분 막음 (일 경계 시 또는 계획이 오래 실행되고 있을 때 문제)
and a.IS_RUNNING = 'Y'
and isnull(a.IS_FINISHED, 'N') != 'E' -- 오류난 것 아닌 것 중에서 정상 실행 중인 것을 조회. 향후에는 오류난 것은 IS_RUNNING 도 'N'으로 설정할 것. (오류 처리되지 않은 것이 남아있으면 
AND ( b.OUTBOUND_FLAG != 'C' OR b.ENGINE_FLAG != 'C' OR  b.INBOUND_FLAG != 'C'  ) -- 아직 완료되지 않은 것 
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchOption(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            sSQL.Append($@"
select 
	WAIT_LEAD_TIME AS [대기 LEAD TIME(%)],
    PROCESSING_LEAD_TIME AS [가공 LEAD TIME(%)],
    DAY_CAPACITY_LIMIT AS [일 물량제약(%)],
    CAST(MASS AS INT) AS MASS,
	CAST(SAMPLE AS INT) AS SAMPLE,
	CAST(TEST AS INT) AS TEST,
    CAST(HOLD_USE AS INT) AS HOLD_USE,
    CAST(JIG_CAPA AS INT) AS JIG_CAPA,
    CAST(MPS_UPLOAD AS INT) AS MPS_UPLOAD,
    CAST(INPUT_PLAN AS INT) AS INPUT_PLAN,
    MATERIAL_CONSTRAINT_YN
from 
	TH_GUI_PLAN_OPTION_PROD_TYPE 
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchOptionDeptBy(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            sSQL.Append($@"
select 
	GUBUN,
	[INPUT],
	E_LESS_CU_PLATING,
	PATTERN_CU_PLATING_FILL AS PATTERN_CU_PLATING,
	M_CZ AS SM_CZ
from TH_GUI_PLAN_OPTION_DEPT_BY
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private void create_version()
        {
            String userID = Cookie<User>.Store.USER_ID;

            //            StringBuilder sSQL = new StringBuilder();

            //            sSQL.Append($@"
            //EXECUTE [dbo].[PR_MANUAL_APS] '{userID}'
            //");

            //            Console.WriteLine(sSQL.ToString());


            //            //HS.Web.Common.Data.Result(sSQL.ToString());
            //            try
            //            {
            //                HS.Web.Common.Data.Execute(sSQL.ToString());
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.ToString());
            //            }

            // 위에 코드로 프로시저 실행시키면 Transaction 관련 에러 발생
            (string dbType, string connection) = Data.GetConnection("Default");

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("PR_MANUAL_APS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        // 파라미터 전달 추가 필요
                        cmd.Parameters.AddWithValue("@p_user_id", userID);
                        cmd.ExecuteNonQuery();
                    }
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void run(Params terms)
        {
            (string dbType, string connection) = Data.GetConnection("Default");

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("PR_MANUAL_RUN_APS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // 파라미터 전달 추가 필요
                        cmd.Parameters.AddWithValue("@p_master_id", "SIMMTECH");
                        cmd.Parameters.AddWithValue("@p_plan_id", terms["PLAN_ID"].AsString());

                        




                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private DataTable Search_His(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            sSQL.Append($@"
select 
	PLAN_ID,
	PLAN_ATTB_3*100 AS ""대기 LEAD TIME"",
	PLAN_ATTB_4*100 AS ""가공 LEAD TIME"",
	PLAN_ATTB_5*100 AS ""일 물량 제약"",
	CASE WHEN PLAN_ATTB_8 = 1 THEN 'Y' ELSE 'N' END AS ""HOLD UES"",
	CASE WHEN PLAN_ATTB_10 = 1 THEN 'Y' ELSE 'N' END AS ""양산"",
	CASE WHEN PLAN_ATTB_11 = 1 THEN 'Y' ELSE 'N' END AS ""샘플"",
	CASE WHEN PLAN_ATTB_12 = 1 THEN 'Y' ELSE 'N' END AS ""테스트"",
	CASE WHEN PLAN_ATTB_13 = 1 THEN 'Y' ELSE 'N' END AS ""JIG CAPA"",
	CASE WHEN PLAN_ATTB_14 = 1 THEN 'Y' ELSE 'N' END AS ""MPS UPLOAD 물량"",
	CASE WHEN PLAN_ATTB_15 = 1 THEN 'Y' ELSE 'N' END AS ""투입계획 수립 현황"",
	CASE WHEN PLAN_ATTB_16 = 'Y' THEN '유한' ELSE '무한' END AS ""자재제약여부""
from 
	TH_MST_PLAN
WHERE
	PLAN_ID = {terms["PLAN_ID"].V}
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable Search_His_DEPT_BY(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            sSQL.Append($@"
select
    A.MASTER_ID,
    A.PLAN_ID,
    A.GUBUN AS GUBUN,
    A.INPUT AS INPUT,
    A.E_LESS_CU_PLATING AS E_LESS_CU_PLATING,
    A.PATTERN_CU_PLATING_FILL AS PATTERN_CU_PLATING_FILL,
    A.M_CZ AS SM_CZ
from
    TH_GUI_PLAN_OPTION_DEPT_BY_HIS A
WHERE 
	PLAN_ID = {terms["PLAN_ID"].V}
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }
        


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            HS.Web.Proc.TH_MST_PLAN.Save(data);
        }

        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveOption(ParamList data)
        {
            HS.Web.Proc.TH_MST_PLAN_OPTION.Save(data);
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveOptionDeptBy(ParamList data)
        {
            HS.Web.Proc.TH_MST_PLAN_OPTION_DEPT_BY.Save(data);
        }
    }
}
