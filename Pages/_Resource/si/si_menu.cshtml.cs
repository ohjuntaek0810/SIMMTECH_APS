using HS.Core;
using HS.Web.Common;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class TH_GUI_MENU : BasePageModel
    {   
        public TH_GUI_MENU()
        {
            this.Handler = handler;       
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.search(terms);
            }

            else if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.search(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("MENU_CD", "메뉴 코드가 입력되지 않았습니다.");
                vali.Null("VIEW_YN", "보이기 여부가 입력되지 않았습니다.");
                
                vali.DoneDeco();

                this.Save(data);


                // 데이터 저장
            }

            return toClient;
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable search(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
SELECT
    A.CMP_CD
    , A.MENU_CD      
    , A.MENU_NM      
    , A.UP_MENU_CD   
    , A.SORT         
    , A.S_URL        
    , A.MENU_NM_DTL  
    , A.TARGET  
    , A.I_CLASS  
    , A.VIEW_YN      
    , A.USE_YN       
    , A.REG_DM       
    , A.REG_ID       
    , A.MDF_DM       
    , A.MDF_ID       

    , UP.MENU_NM            AS UP_MENU_NM

FROM
    TH_GUI_MENU A
LEFT JOIN TH_GUI_MENU UP
    ON UP.MENU_CD = A.UP_MENU_CD
WHERE 1 = 1
AND A.CMP_CD = " + terms["CLIENT"].V + @"
AND A.USE_YN = 'Y'
");

            if (terms["search"].Length > 0)
            {
                terms["search"] = terms["search"].AsString().Trim();
                List<string> searchTermsList = terms["search"].AsString().Split(" ").ToList();

                int index = 0;

                sSQL.Append(@"
AND
(
");
                searchTermsList.ForEach(search =>
                {
                    if (index == 0)
                    {
                        sSQL.Append($@"
    (
        (A.MENU_CD LIKE '%{search}%') OR
        (A.MENU_NM LIKE '%{search}%') 
    )
");
                    }
                    else
                    {
                        sSQL.Append($@"
    OR 
    (
        (A.MENU_CD LIKE '%{search}%') OR
        (A.MENU_NM LIKE '%{search}%') 
    )
");
                    }

                    index++;
                });

                sSQL.Append(@"
)
");
            }

            if (terms["MENU_CD"].Length > 0)
            {
                sSQL.Append($@"
AND A.MENU_CD = {terms["MENU_CD"].V}
");
                return Data.Get(sSQL.ToString()).Tables[0];
            }
            sSQL.Append(@"
ORDER BY A.SORT
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            HS.Web.Proc.TH_GUI_MENU.Save(data);
        }

    }
}
