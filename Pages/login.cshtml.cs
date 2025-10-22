using HS.Core;
using HS.Web.Common;
using HS.Web.Service;
using Microsoft.AspNetCore.Mvc;

namespace HS.Web.Pages
{
    public class loginModel : BasePageModel
    {
        Microsoft.AspNetCore.Http.IHttpContextAccessor acc;

        public loginModel(Microsoft.AspNetCore.Http.IHttpContextAccessor acc)
        {
            this.Handler = handler;
            this.OnGetHandler = OnGetHS;
            this.IsSessionRequire = false;
            this.acc = acc;
        }

        private IActionResult OnGetHS(GetArgs arg)
        {
            // 이전 페이지 url 있으면
            if (arg.Params["Ref"].AsString() != "")
                this.Params["Ref"] = arg.Params["Ref"];

            this.Params["dt"] = arg.Params["dt"]; // 오성전용타입

            return Page();
        }

        public Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "login")
            {
                Params terms = e.Params["terms"];

                // 암호화된 패스워드 비교를 위한 변경작업
                terms["PASSWD"] = Encryption.SHA256Hash(terms["PASSWD"].AsString());

                toClient["data"] = DTClient.Login(this.acc, terms);
            }

            if (e.Command == "logout")
            {
                this.IsSessionRequire = false;
                DTClient.Logout();
            }

            return toClient;
        }
    }
}
