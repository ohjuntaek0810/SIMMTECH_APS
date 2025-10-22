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
    public class lookup : BasePageModel
    {
        public lookup()
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

            else if (e.Command == "search_detail")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchValue(terms);
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];
                
                // TYPE 데이터 저장
                this.Save(dataList);
            }

            else if (e.Command == "save_detail")
            {
                ParamList dataList = e.Params["data"];

                // VALUE 데이터 저장
                this.SaveDetail(dataList);
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
SELECT
    ltm.LOOKUP_TYPE_CODE AS ORIGN_LOOKUP_TYPE_CODE,
	ltm.LOOKUP_TYPE_CODE,
	ltm.LOOKUP_TYPE_VERSION,
	ltm.ACTIVE_FLAG,
	ltm.IS_LATEST_VERSION_YN,
	ltm.VALUE_TABLE_TYPE,
	ltm.SEGMENT1,
	ltm.SEGMENT2,
	ltm.SEGMENT3,
	ltm.SEGMENT4,
	ltm.SEGMENT5,
	ltm.SEGMENT6,
	ltm.SEGMENT7,
	ltm.SEGMENT8,
	ltm.ATTRIBUTE01,
	ltm.ATTRIBUTE02,
	ltm.ATTRIBUTE03,
	ltm.ATTRIBUTE04,
	ltm.ATTRIBUTE05,
	ltm.ATTRIBUTE06,
	ltm.ATTRIBUTE07,
	ltm.ATTRIBUTE08,
	ltm.ATTRIBUTE09,
	ltm.ATTRIBUTE10,
	ltm.ATTRIBUTE11,
	ltm.ATTRIBUTE12,
	ltm.ATTRIBUTE13,
	ltm.ATTRIBUTE14,
	ltm.ATTRIBUTE15,
	ltm.INSERT_ID,
	ltm.INSERT_DTTM,
	ltm.UPDATE_ID,
	ltm.UPDATE_DTTM,
	ltm.TYPE_DESCRIPTION 
FROM
	LOOKUP_TYPE_M ltm
WHERE
    1=1
");
            /*
             * 조건절 시작
             */

            // ACTIVE_FLAG
            if (terms["active_flag"].Length > 0)
            {
                sSQL.Append($@"
    AND ltm.ACTIVE_FLAG = '{terms["active_flag"].AsString()}'
");
            }

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchValue(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT 
	LVM.LOOKUP_TYPE_CODE,
	LVM.LOOKUP_TYPE_VERSION,
	LVM.ACTIVE_FLAG,
	LVM.IS_LATEST_VERSION_YN,
	LVM.SORT_ORDER,
	LVM.VALUE_DESCRIPTION,
	LVM.SEGMENT1,
	LVM.SEGMENT2,
	LVM.SEGMENT3,
	LVM.SEGMENT4,
	LVM.SEGMENT5,
	LVM.SEGMENT6,
	LVM.SEGMENT7,
	LVM.SEGMENT8,
    LVM.SEGMENT1 AS ORIGN_SEGMENT1,
	LVM.SEGMENT2 AS ORIGN_SEGMENT2,
	LVM.SEGMENT3 AS ORIGN_SEGMENT3,
	LVM.SEGMENT4 AS ORIGN_SEGMENT4,
	LVM.SEGMENT5 AS ORIGN_SEGMENT5,
	LVM.SEGMENT6 AS ORIGN_SEGMENT6,
	LVM.SEGMENT7 AS ORIGN_SEGMENT7,
	LVM.SEGMENT8 AS ORIGN_SEGMENT8,
	LVM.ATTRIBUTE01,
	LVM.ATTRIBUTE02,
	LVM.ATTRIBUTE03,
	LVM.ATTRIBUTE04,
	LVM.ATTRIBUTE05,
	LVM.ATTRIBUTE06,
	LVM.ATTRIBUTE07,
	LVM.ATTRIBUTE08,
	LVM.ATTRIBUTE09,
	LVM.ATTRIBUTE10,
	LVM.ATTRIBUTE11,
	LVM.ATTRIBUTE12,
	LVM.ATTRIBUTE13,
	LVM.ATTRIBUTE14,
	LVM.ATTRIBUTE15,
	LVM.INSERT_ID,
	LVM.INSERT_DTTM,
	LVM.UPDATE_ID,
	LVM.UPDATE_DTTM
FROM
	LOOKUP_VALUE_M	LVM
WHERE
	1=1
");
            /*
             * 조건절 시작
             */
            if (terms["LOOKUP_TYPE_CODE"].Length > 0) // LOOKUP_TYPE_CODE 조건
            {
                sSQL.Append($@"
    AND LVM.LOOKUP_TYPE_CODE = {terms["LOOKUP_TYPE_CODE"].V}
");
            }
            if (terms["LOOKUP_TYPE_VERSION"].Length > 0) // LOOKUP_TYPE_VERSION 조건
            {
                sSQL.Append($@"
    AND LVM.LOOKUP_TYPE_VERSION = {terms["LOOKUP_TYPE_VERSION"].V}
");
            }

            sSQL.Append($@"
ORDER BY LVM.SORT_ORDER
");


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
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList dataList)
        {
            HS.Web.Proc.LOOKUP_TYPE_M.Save(dataList);
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
