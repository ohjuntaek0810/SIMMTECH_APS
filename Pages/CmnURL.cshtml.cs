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

            // ����ȭ grid ����
            if (e.Command == "save_grid")
            {
                ParamList dataList = e.Params["data"];

                CmnClient.SaveGrid(dataList);
            }

            // ����ȭ grid ��ȸ
            if (e.Command == "search_grid")
            {
                Params data = e.Params["terms"];

                toClient["data"] = CmnClient.SearchGrid(data);
            }

            // ���ã�� check
            if (e.Command == "check_favorite")
            {
                Params data = e.Params["terms"];

                toClient["data"] = CmnClient.checkFavorite(data);

            }

            // ���ã�� �߰�
            if (e.Command == "add_favorite")
            {
                Params data = e.Params["terms"];

                CmnClient.addFavorite(data);
            }

            // ���ã�� ����
            if (e.Command == "delete_favorite")
            {
                Params data = e.Params["terms"];

                CmnClient.deleteFavorite(data);
            }

            return toClient;
        }
    }
}
