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
    public class input_daily_production_target : BasePageModel
    {
        public input_daily_production_target()
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
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];
                
                this.Save(dataList);
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
with 
WRG_IO_WITH_PATTERN_TYPE_L1 as (
select A.DIVISION_ID, A.APS_WIP_ROUTE_GRP_ID, A.WIP_ROUTE_GROUP_NAME, A.LAYER_INOUT, A.WRG_IO_ID, A.WRG_IO_NAME, A.SORT_ORDER, B.PATTERN_TYPE_L1
from WIP_ROUTE_GROUP_INOUT_V A
 inner join 
 PATTER_TYPE_L1_LIST_V B
 on A.DIVISION_ID = B.DIVISION_ID
) 
select 
	A.DIVISION_ID AS ""GROUP"", 
	A.APS_WIP_ROUTE_GRP_ID, 
	A.WIP_ROUTE_GROUP_NAME, 
	A.LAYER_INOUT, --B.LAYER_INOUT as B_LAYER_INOUT, 
	A.WRG_IO_ID, 
	A.WRG_IO_NAME, 
	A.PATTERN_TYPE_L1, --B.PATTERN_TYPE_L1 as TT, 
	B.DAILY_PRODUCTION_TARGET_QTY, 
	A.SORT_ORDER 
from 
	WRG_IO_WITH_PATTERN_TYPE_L1 A
	left outer join 
	TH_TAR_WRG_IO_PTN_DAILY_PROD_TARGET B
	on 1=1
	and A.DIVISION_ID = B.DIVISION_ID
	and A.APS_WIP_ROUTE_GRP_ID = B.APS_WIP_ROUTE_GRP_ID
	and A.LAYER_INOUT = B.LAYER_INOUT
	and A.PATTERN_TYPE_L1 = B.PATTERN_TYPE_L1
	and B.use_yn = 'Y'
where 
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

            // aps_wip_route_group_name
            if (terms["aps_wip_route_group_name"].Length > 0)
            {
                // terms["aps_wip_route_group_name"] 데이터 짤라서 사용해야함
                string[] wip_list = terms["aps_wip_route_group_name"].AsString().Split(",");
                List<string> wip_result_list = new List<string>();

                foreach (var item in wip_list)
                {
                    wip_result_list.Add($@"'{item}'");
                }

                string wip_result = string.Join(',', wip_result_list);

                sSQL.Append($@"
    AND A.APS_WIP_ROUTE_GRP_ID IN ({wip_result})
");
            }

            // own_out_gbn
            if (terms["own_out_gbn"].Length > 0)
            {
                if (terms["own_out_gbn"].AsString() == "IN")
                {
                    sSQL.Append($@"
	AND A.LAYER_INOUT = 'INNER'
");
                }
                else
                {
                    sSQL.Append($@"
	AND A.LAYER_INOUT = 'OUTER'
");
                }
            }

            // 대분류
            if (terms["category_level1"].Length > 0)
            {
                sSQL.Append($@"
	AND A.PATTERN_TYPE_L1 = {terms["category_level1"].V}
");
            }



            sSQL.Append($@"
order by A.DIVISION_ID,  A.LAYER_INOUT, A.SORT_ORDER, A.PATTERN_TYPE_L1 
");

            Console.WriteLine(sSQL.ToString());


            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList dataList)
        {
            HS.Web.Proc.TH_TAR_WRG_IO_PTN_DAILY_PROD_TARGET.Save(dataList);
        }
    }
}
