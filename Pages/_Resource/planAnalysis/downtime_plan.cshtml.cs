using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class downtime_plan : BasePageModel
    {
        public downtime_plan()
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                Params terms = e.Params["terms"];
                ParamList dataList = e.Params["data"];
                ValiList vali = new ValiList(dataList);
                vali.Null("APS_RESOURCE_ID", "APS_RESOURCE_ID가 입력되지 않았습니다.");
                vali.Null("RESOURCE_CAPA_GROUP_ID", "RESOURCE_CAPA_GROUP_ID가 입력되지 않았습니다.");
                vali.Null("OFF_DATE", "OFF_DATE가 입력되지 않았습니다.");
                vali.Null("OFF_SHIFT", "OFF_SHIFT가 입력되지 않았습니다.");
                vali.DoneDeco();

                this.Save(dataList);
                toClient["data"] = this.Search(terms);

                // 데이터 저장
            }
            else if (e.Command == "delete")
            {
                Params terms = e.Params["terms"];
                ParamList dataList = e.Params["data"];
                this.delete(dataList);
                toClient["data"] = this.Search(terms);

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

            sSQL.Append($@"
SELECT * FROM  TH_TAR_RESOURCE_OFF_PLAN A with (nolock) 
WHERE 1=1
AND A.OFF_DATE between {terms["start_date"].V} and  {terms["end_date"].V}

");

            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
                ");
            }
            if (terms["RESOURCE_CAPA_GROUP_ID"].Length > 0)
            {
                sSQL.Append($@"
                    AND A.RESOURCE_CAPA_GROUP_ID = '{terms["RESOURCE_CAPA_GROUP_ID"].AsString()}'
                ");
            }
            sSQL.Append($@"
ORDER BY A.DIVISION_ID , A.APS_RESOURCE_ID , A.OFF_DATE
");
            Console.WriteLine(sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0]; 
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private Params search_chart(Params terms)
        {
            Params result = new();

            return result;
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
      
        private void Save(ParamList dataList)
        {
            StringBuilder sSQL = new StringBuilder();


            foreach (Params ITEM in dataList)
            {
                //변수 선언
                sSQL.Append($@" 
                    DECLARE @DIVISION_ID NVARCHAR(150) = {ITEM["DIVISION_ID"].V};
                    DECLARE @APS_RESOURCE_ID NVARCHAR(300) = {ITEM["APS_RESOURCE_ID"].V};
                    DECLARE @RESOURCE_CAPA_GROUP_ID NVARCHAR(200) =  {ITEM["RESOURCE_CAPA_GROUP_ID"].V};
                    DECLARE @RESOURCE_CAPA_GROUP_NAME NVARCHAR(200) = {ITEM["RESOURCE_CAPA_GROUP_NAME"].V};
                    DECLARE @SITE_ID NVARCHAR(200) = {ITEM["SITE_ID"].V};
                    DECLARE @RESOURCE_CNT INT = {ITEM["RESOURCE_CNT"].V};
                    DECLARE @APPLY_CAPA  float =  {ITEM["APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29"].V} ;                   
                    DECLARE @OFF_DATE DATETIME = {ITEM["OFF_DATE"].V};
                    DECLARE @OFF_SHIFT NVARCHAR(10) = {ITEM["OFF_SHIFT"].V};
                    DECLARE @CAPA_LOSS FLOAT = {ITEM["CAPA_LOSS"].V};
                    DECLARE @REMARK NVARCHAR(2000) = {ITEM["REMARK"].V};
                    DECLARE @USE_YN CHAR(1) = 'Y';
                    DECLARE @USERID VARCHAR(50) = '';
                ");

                              
                sSQL.Append($@"  
                    -- 계산 변수
                    DECLARE @OFF_RATE FLOAT = IIF(@APPLY_CAPA > 0, ROUND(@CAPA_LOSS / @APPLY_CAPA, 6), 0);
                    SET @OFF_RATE = IIF(@OFF_RATE > 1, 1, @OFF_RATE);

                    DECLARE @OFF_DATE_START_DTTM DATETIME = DATEADD(MINUTE, 450, @OFF_DATE); -- 당일 7시간 30분
                    DECLARE @OFF_DATE_END_DTTM DATETIME = DATEADD(MINUTE, 1440+450, @OFF_DATE); -- 익일 7시30분
                    
                    DECLARE @OFF_START_DTTM datetime;
                    DECLARE @OFF_END_DTTM datetime;

                    -- Shift에 따라 시작/종료 시간 계산
                    IF @OFF_SHIFT = 'DAY'
                    BEGIN
                        SET @OFF_START_DTTM = @OFF_DATE_START_DTTM;
                        SET @OFF_END_DTTM = DATEADD(MINUTE, @OFF_RATE * 1440, @OFF_DATE_START_DTTM);
                    END                    
                    ELSE IF @OFF_SHIFT = 'NIGHT'
                    BEGIN
                        SET @OFF_START_DTTM = DATEADD(MINUTE, -@OFF_RATE * 1440, @OFF_DATE_END_DTTM);
                        SET @OFF_END_DTTM = @OFF_DATE_END_DTTM;
                    END;                 

                    -- 최대값 1로 제한
                    SET @OFF_RATE = IIF(@OFF_RATE > 1, 1, @OFF_RATE);
                ");

                sSQL.Append($@"                 

                    MERGE INTO TH_TAR_RESOURCE_OFF_PLAN AS TARGET
                    USING (
                        SELECT 
                            @DIVISION_ID AS DIVISION_ID,
                            @APS_RESOURCE_ID AS APS_RESOURCE_ID,
                            @RESOURCE_CAPA_GROUP_ID AS RESOURCE_CAPA_GROUP_ID,
                            @RESOURCE_CAPA_GROUP_NAME AS RESOURCE_CAPA_GROUP_NAME,
                            @SITE_ID AS SITE_ID,
                            @RESOURCE_CNT AS RESOURCE_CNT,
                            @APPLY_CAPA AS APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29,
                            @OFF_DATE AS OFF_DATE,
                            @OFF_SHIFT AS OFF_SHIFT,
                            @CAPA_LOSS AS CAPA_LOSS,
                            @REMARK AS REMARK,
                            @OFF_RATE AS OFF_RATE,
                            @OFF_START_DTTM AS OFF_START_DTTM,
                            @OFF_END_DTTM AS OFF_END_DTTM,
                            'Y' AS USE_YN,
                            @USERID AS INSERT_ID,                            
                            @USERID AS UPDATE_ID                          
                    ) AS SOURCE
                    ON (
                        TARGET.DIVISION_ID = SOURCE.DIVISION_ID AND
                        TARGET.APS_RESOURCE_ID = SOURCE.APS_RESOURCE_ID AND
                        TARGET.OFF_DATE = SOURCE.OFF_DATE AND
                        TARGET.OFF_SHIFT = SOURCE.OFF_SHIFT
                    )
                    WHEN MATCHED THEN
                        UPDATE SET 
                            TARGET.RESOURCE_CAPA_GROUP_ID = SOURCE.RESOURCE_CAPA_GROUP_ID,
                            TARGET.RESOURCE_CAPA_GROUP_NAME = SOURCE.RESOURCE_CAPA_GROUP_NAME,
                            TARGET.SITE_ID = SOURCE.SITE_ID,
                            TARGET.RESOURCE_CNT = SOURCE.RESOURCE_CNT,
                            TARGET.APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29 = SOURCE.APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29,
                            TARGET.CAPA_LOSS = SOURCE.CAPA_LOSS,
                            TARGET.REMARK = SOURCE.REMARK,
                            TARGET.OFF_RATE = SOURCE.OFF_RATE,
                            TARGET.OFF_START_DTTM = SOURCE.OFF_START_DTTM,
                            TARGET.OFF_END_DTTM = SOURCE.OFF_END_DTTM,
                            TARGET.USE_YN = SOURCE.USE_YN,
                            TARGET.UPDATE_ID = SOURCE.UPDATE_ID,
                            TARGET.UPDATE_DTTM = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (
                            DIVISION_ID,
                            APS_RESOURCE_ID,
                            RESOURCE_CAPA_GROUP_ID,
                            RESOURCE_CAPA_GROUP_NAME,
                            SITE_ID,
                            RESOURCE_CNT,
                            APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29,
                            OFF_DATE,
                            OFF_SHIFT,
                            CAPA_LOSS,
                            REMARK,
                            OFF_RATE,
                            OFF_START_DTTM,
                            OFF_END_DTTM,
                            USE_YN,
                            INSERT_ID,
                            INSERT_DTTM                            
                        )
                        VALUES (
                            SOURCE.DIVISION_ID,
                            SOURCE.APS_RESOURCE_ID,
                            SOURCE.RESOURCE_CAPA_GROUP_ID,
                            SOURCE.RESOURCE_CAPA_GROUP_NAME,
                            SOURCE.SITE_ID,
                            SOURCE.RESOURCE_CNT,
                            SOURCE.APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29,
                            SOURCE.OFF_DATE,
                            SOURCE.OFF_SHIFT,
                            SOURCE.CAPA_LOSS,
                            SOURCE.REMARK,
                            SOURCE.OFF_RATE,
                            SOURCE.OFF_START_DTTM,
                            SOURCE.OFF_END_DTTM,
                            SOURCE.USE_YN,
                            SOURCE.INSERT_ID,
                            GETDATE()     
                        );
					");
            }

          

            HS.Web.Common.Data.Execute(sSQL.ToString());

        }



        /// <summary>
        /// 선택한 항목 삭제
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            StringBuilder sSQL = new StringBuilder();
            data.ForEach(ITEM =>
            {
                sSQL.Append($@"
              DELETE FROM TH_TAR_RESOURCE_OFF_PLAN        
               WHERE DIVISION_ID = {ITEM["DIVISION_ID"].V} 
                 AND APS_RESOURCE_ID = {ITEM["APS_RESOURCE_ID"].V} 
                 AND OFF_DATE = {ITEM["OFF_DATE"].V} 
                 AND OFF_SHIFT = {ITEM["OFF_SHIFT"].V}               
                ;");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        /// <summary>
        /// 저장된 그리드 헤더컬럼 가져오기
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private DataTable SearchGrid(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            string USER_ID = "admin";
            string GRID_ID = terms["grid_id"].AsString();

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
	SUG.COLUMN_NAME AS dataField,
    SUG.COLUMN_NAME AS label,
	SUG.VISIBLE AS visible,
	SUG.WIDTH AS width,
	SUG.FIX AS fixed,
	SUG.EDITABLE AS editable
FROM
	TH_GUI_USER_GRID SUG
WHERE
	1=1
	AND SUG.USER_ID = '{USER_ID}'
	AND SUG.GRID_ID = '{GRID_ID}'
ORDER BY SUG.COLUMN_ORDER
");

            return Data.Get(sSQL.ToString()).Tables[0];

        }



        /// <summary>
        /// 그리드 헤더컬럼 옵션 저장
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveGrid(ParamList dataList)
        {
            HS.Web.Proc.SI_GRID.Save(dataList);
        }


        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            if (command == "ExcelDownload")
            {
                //데이터 조회한 값으로 엑셀 다운로드
                DataTable dtResult = this.Search(e.Params["terms"]);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Lot_Routing_Sequence_" + timestamp);
            }
            else
                return Page();
        }
    }
}

/*
 --검증 쿼리문

select  case when A.OFF_SHIFT = 'DAY' then A.OFF_DATE_START_DTTM2 
			 when A.OFF_SHIFT = 'NIGHT' then DATEADD(MINUTE, -off_rate2*1440, A.OFF_DATE_END_DTTM2) 
			 else null end as OFF_START_DTTM2,   -- 적용 Resource Off 시작시간 --> UI에서 표시 가능
		case when A.OFF_SHIFT = 'DAY' then DATEADD(MINUTE, off_rate2*1440, A.OFF_DATE_START_DTTM2) 
			 when A.OFF_SHIFT = 'NIGHT' then A.OFF_DATE_END_DTTM2 
			 else null end as OFF_END_DTTM2, --  적용 Resource Off 종료시간 --> UI에서 표시 가능
		A.*
from (
		select	A.DIVISION_ID, A.APS_RESOURCE_ID, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.SITE_ID, A.RESOURCE_CNT, A.APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29, 
				A.OFF_DATE, 
				--CONVERT(DATE, DATEADD(MINUTE, -450, A.OFF_DATE)) AS BASE_DATE, -- 시업시간 기준 출고가능일 	
				DATEADD(MINUTE, 450, A.OFF_DATE) as OFF_DATE_START_DTTM2,
				DATEADD(MINUTE, 1440+450, A.OFF_DATE) as OFF_DATE_END_DTTM2,
				A.OFF_SHIFT, CAPA_LOSS, REMARK, USE_YN,
				A.OFF_RATE,
				A.OFF_START_DTTM,
				A.OFF_END_DTTM,
			    LEAST(1, case when APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29 > 0 then isnull(CAPA_LOSS, 0) / APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29 else 0 end ) as off_rate2
		from   TH_TAR_RESOURCE_OFF_PLAN A with (nolock)
	 ) A



*/
