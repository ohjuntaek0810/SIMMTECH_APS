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

                // ������ ����
                this.Save(dataList);
            }

            else if (e.Command == "save_option")
            {
                ParamList dataList = e.Params["data"];
                ParamList dataList_dept_by = e.Params["data_dept_by"];


                Console.WriteLine(dataList);

                // ������ ����
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
        /// ��ȸ ���� 
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
      , CASE WHEN A.IS_RUNNING = 'Y'  and B.OUTBOUND_FLAG = 'C' and A.IS_FINISHED is null and datediff(second, B.OUTBOUND_END, getdate()) > 60  THEN  'ERROR' --## PR_BAT_APS���� 1�и��� ���� ���Ḧ üũ�ϱ� ���� ERROR�� ǥ�õǴ� ���� ����
        WHEN A.IS_RUNNING = 'N' and A.IS_FINISHED = 'E' then 'ERROR'  --##  2025-10-15 �߰�. �������� ���� ó�� ��, IS_RUNNING = 'N',  IS_FINISHED = 'E'�� ������Ʈ �� ��. 
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
  -- ������ 
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
-- �������� ��ȹ�� �ִ��� Ȯ���ϴ� ���� --> ����� ������ �������ΰ� ����. 
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
      , CASE WHEN A.IS_RUNNING = 'Y' and A.IS_FINISHED is null and datediff(second, B.OUTBOUND_END, getdate()) > 60  THEN  'ERROR' --## 2025-09-30 ����. PR_BAT_APS���� 1�и��� ���� ���Ḧ üũ�ϱ� ���� ERROR�� ǥ�õǴ� ���� ���� 
             WHEN A.IS_RUNNING = 'N' and A.IS_FINISHED = 'E' then 'ERROR'  --##  2025-10-15 �߰�. �������� ���� ó�� ��, IS_RUNNING = 'N',  IS_FINISHED = 'E'�� ������Ʈ �� ��. 
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
  -- ������ 
  --, A.IS_RUNNING
  --, A.IS_FINISHED 
FROM TH_MST_PLAN  A 
	inner join
	TH_MST_ENG_PLAN_FLAG B 
	on A.plan_id = B.plan_id 
WHERE A.MASTER_ID = 'SIMMTECH'
--AND   A.PLAN_ID   LIKE '%20251014%'  -- ���� �������� Plan ������ ��ü �Ⱓ ��ȸ �ؾ� �� 
--and   CONVERT(date, B.INSERT_DTTM) = CONVERT(date, GETDATE())  -- ������ ������ �͸� ������� �����ϴ� �κ� ���� (�� ��� �� �Ǵ� ��ȹ�� ���� ����ǰ� ���� �� ����)
and a.IS_RUNNING = 'Y'
and isnull(a.IS_FINISHED, 'N') != 'E' -- ������ �� �ƴ� �� �߿��� ���� ���� ���� ���� ��ȸ. ���Ŀ��� ������ ���� IS_RUNNING �� 'N'���� ������ ��. (���� ó������ ���� ���� ���������� 
AND ( b.OUTBOUND_FLAG != 'C' OR b.ENGINE_FLAG != 'C' OR  b.INBOUND_FLAG != 'C'  ) -- ���� �Ϸ���� ���� �� 
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchOption(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();


            sSQL.Append($@"
select 
	WAIT_LEAD_TIME AS [��� LEAD TIME(%)],
    PROCESSING_LEAD_TIME AS [���� LEAD TIME(%)],
    DAY_CAPACITY_LIMIT AS [�� ��������(%)],
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
        /// ��ȸ ���� 
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

            // ���� �ڵ�� ���ν��� �����Ű�� Transaction ���� ���� �߻�
            (string dbType, string connection) = Data.GetConnection("Default");

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand("PR_MANUAL_APS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        // �Ķ���� ���� �߰� �ʿ�
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

                        // �Ķ���� ���� �߰� �ʿ�
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
	PLAN_ATTB_3*100 AS ""��� LEAD TIME"",
	PLAN_ATTB_4*100 AS ""���� LEAD TIME"",
	PLAN_ATTB_5*100 AS ""�� ���� ����"",
	CASE WHEN PLAN_ATTB_8 = 1 THEN 'Y' ELSE 'N' END AS ""HOLD UES"",
	CASE WHEN PLAN_ATTB_10 = 1 THEN 'Y' ELSE 'N' END AS ""���"",
	CASE WHEN PLAN_ATTB_11 = 1 THEN 'Y' ELSE 'N' END AS ""����"",
	CASE WHEN PLAN_ATTB_12 = 1 THEN 'Y' ELSE 'N' END AS ""�׽�Ʈ"",
	CASE WHEN PLAN_ATTB_13 = 1 THEN 'Y' ELSE 'N' END AS ""JIG CAPA"",
	CASE WHEN PLAN_ATTB_14 = 1 THEN 'Y' ELSE 'N' END AS ""MPS UPLOAD ����"",
	CASE WHEN PLAN_ATTB_15 = 1 THEN 'Y' ELSE 'N' END AS ""���԰�ȹ ���� ��Ȳ"",
	CASE WHEN PLAN_ATTB_16 = 'Y' THEN '����' ELSE '����' END AS ""�������࿩��""
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
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            HS.Web.Proc.TH_MST_PLAN.Save(data);
        }

        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveOption(ParamList data)
        {
            HS.Web.Proc.TH_MST_PLAN_OPTION.Save(data);
        }


        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveOptionDeptBy(ParamList data)
        {
            HS.Web.Proc.TH_MST_PLAN_OPTION_DEPT_BY.Save(data);
        }
    }
}
