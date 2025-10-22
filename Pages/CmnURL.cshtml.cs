using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace APS_SIMMTECH.Pages
{
    public class CmnURLModel : BasePageModel
    {
        public CmnURLModel()
        {
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            // 개인화 grid 저장
            if (e.Command == "save_grid")
            {
                ParamList dataList = e.Params["data"];

                CmnClient.SaveGrid(dataList);
            }

            // 개인화 grid 조회
            if (e.Command == "search_grid")
            {
                Params data = e.Params["terms"];

                toClient["data"] = CmnClient.SearchGrid(data);
            }

            // 즐겨찾기 check
            if (e.Command == "check_favorite")
            {
                Params data = e.Params["terms"];

                toClient["data"] = CmnClient.checkFavorite(data);

            }

            // 즐겨찾기 추가
            if (e.Command == "add_favorite")
            {
                Params data = e.Params["terms"];

                CmnClient.addFavorite(data);
            }

            // 즐겨찾기 삭제
            if (e.Command == "delete_favorite")
            {
                Params data = e.Params["terms"];

                CmnClient.deleteFavorite(data);
            }

            return toClient;
        }
    }
}
