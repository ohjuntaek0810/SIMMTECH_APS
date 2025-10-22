using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class item_by_bottle_neck : BasePageModel
    {
        public item_by_bottle_neck()
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

                // 데이터 저장
                this.Save(dataList);


                // 데이터 저장
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
select 
    * 
from  
    TH_TAR_RCG_ITEM_DIVISION with (nolock)
WHERE
    1=1
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
    AND DIVISION_ID = {terms["group_id"].V}
");
            }

            if (terms["group_gubun"].Length > 0)
            {
                sSQL.Append($@"
    AND GROUP_GUBUN = {terms["group_gubun"].V}
");
            }

            if (terms["bottle_neck_name"].Length > 0)
            {
                sSQL.Append($@"
    AND BOTTLE_NECK_ID = {terms["bottle_neck_name"].V}
");
            }

            if (terms["item_code"].Length > 0)
            {
                sSQL.Append($@"
    AND ITEM_CODE LIKE '%{terms["item_code"].AsString()}%'
");
            }

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            //HS.Web.Proc.TH_GUI_READY_BY_INPUT_MATERIAL_REMARK.Save(data);
        }
    }
}
