using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class aps_resource_group_capa : BasePageModel
    {
        public aps_resource_group_capa()
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
                ParamList dataList = e.Params["data"];
                
                this.Save(dataList);
            }
            else if(e.Command == "delete")
            {
                ParamList data = e.Params["data"];


                this.delete(data);
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
select	
	A.DIVISION_ID AS ""GROUP"", 
	A.RESOURCE_CAPA_GROUP_ID AS ""CAPA GROUP ID"", 
	A.RESOURCE_CAPA_GROUP_NAME AS ""CAPA GROUP NAME"", 
	A.OUTSOURCE_YN, 
	A.OWN_OUT_GBN AS ""IN/OUT"", 
	A.RESOURCE_MODELING_YN, 
	A.RESOURCE_LEVEL, 
	A.LEVEL2_GBN, 
	A.LEVEL2_SORT_ORDER, 
	A.SITE_ID AS ""SITE"", 
	A.APS_RESOURCE_ID AS ""RESOURCE CAPA ID"", 
	A.APS_RESOURCE_NAME AS ""RESOURCE CAPA NAME"", 
	CEILING(A.TOTAL_SITE_M2_MONTH_PROD) AS ""CAPA/MONTH"", 
	CEILING(A.TOTAL_SITE_RCG_CAPA_M2_DAY_29) AS ""CAPA/DAY"", 
	A.MAX_CAPA_REV, 
	A.RES_CNT, 
	A.AVG_SITE_RCG_CAPA_M2_DAY_29, 
	B.USER_TOTAL_SITE_RCG_CAPA_M2_DAY_29 AS ""USE CAPA/DAY"", 
	isnull(B.USE_YN, 'Y') AS ""USE(Y/N)"",
	B.INSERT_ID AS ""INSERT ID"",
	FORMAT(B.INSERT_DTTM, 'yyyy-MM-dd HH:mm') AS ""INSERT DATE"",
	B.UPDATE_ID AS ""UPDATE ID"",
	FORMAT(B.UPDATE_DTTM, 'yyyy-MM-dd HH:mm')  AS ""UPDATE DATE""
from 
	APS_SITE_RESOURCE_MES_CAPA_V A
	left outer join 
	TH_TAR_SITE_RCG_CAPA_BY_USER B
	on A.APS_RESOURCE_ID = B.APS_RESOURCE_ID
-- where isnull(B.USE_YN, 'Y') = 'Y' -->  엔진 로직 사용 시 조건 추가 (USE_YN = 'N'이면 해당 APS_RESOURCE_ID에 대한 TOTAL_RCG_CAPA_M2_DAY_29, USER_TOTAL_SITE_RCG_CAPA_M2_DAY_29 둘 다 적용하지 않음. Capa 없는 것으로 설정)
--where a.aps_resource_id = 'APS_RCG_0001_F52'
WHERE
    1=1
");
            

            /*
             * 조건절 시작
             */

            // 사업부
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
    AND A.DIVISION_ID = '{terms["group_id"].AsString()}'
");
            }

            // RESOURCE_CAPA_GROUP
            if (terms["resource_capa_group"].Length > 0)
            {
                sSQL.Append($@"
    AND A.RESOURCE_CAPA_GROUP_NAME = {terms["resource_capa_group"].V}
");
            }

            // own_out_gbn
            if (terms["own_out_gbn"].Length > 0)
            {
                sSQL.Append($@"
    AND A.OWN_OUT_GBN LIKE '%{terms["own_out_gbn"].AsString()}%'
");
            }


            // use_yn
            if (terms["use_yn"].Length > 0)
            {
                sSQL.Append($@"
    AND isnull(B.USE_YN, 'Y') =  {terms["use_yn"].V}
");
            }



            sSQL.Append($@"
order by A.DIVISION_ID, A.RESOURCE_CAPA_GROUP_ID, A.RESOURCE_CAPA_GROUP_NAME, A.LEVEL2_SORT_ORDER, A.SITE_ID, A.APS_RESOURCE_ID
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList dataList)
        {
            HS.Web.Proc.TH_TAR_RCG_CAPA_L.Save(dataList);
        }

        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveDetail(ParamList dataList)
        {
            HS.Web.Proc.LOOKUP_VALUE_M.Save(dataList);
        }

        /// <summary>
        /// 선택한 항목 삭제
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            throw new Exception("준비중입니다.");

            StringBuilder sSQL = new StringBuilder();

            data.ForEach(D =>
            {
                sSQL.Append($@"
DELETE FROM SI_CODE_GROUP WHERE CMP_CD = {D["CMP_CD"].V} AND GRP_CD = {D["GRP_CD"].V};
");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
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
