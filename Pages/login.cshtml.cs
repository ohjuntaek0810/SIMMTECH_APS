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
            // ���� ������ url ������
            if (arg.Params["Ref"].AsString() != "")
                this.Params["Ref"] = arg.Params["Ref"];

            this.Params["dt"] = arg.Params["dt"]; // ��������Ÿ��

            return Page();
        }

        public Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "login")
            {
                Params terms = e.Params["terms"];

                // ��ȣȭ�� �н����� �񱳸� ���� �����۾�
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
