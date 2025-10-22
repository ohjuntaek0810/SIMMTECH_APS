using HS.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HS.Web.Common
{
    public class GetArgs : EventArgs
    {
        public string Command { get; set; }

        public Params Params { get; set; }
    }

    public class PostArgs : EventArgs
    {
        public string Command { get; set; }
        public Params Params { get; set; }
        public List<IFormFile> Files { get; set; }
    }

    public class BasePageModel : HSPageModel
    {
        public Func<GetArgs, IActionResult> OnGetHandler;
        public Func<PostArgs, IActionResult> OnPostHandler;
        public Func<PostArgs, IActionResult> OnPostFetchHandler;

        public string PathName = "";
        public bool IsAjax = false;
        public bool IsSessionRequire = true; // 추후 로그인 기능 완료되면 true로 수정 필요

        private ParamList menuAuth;
        private Params pageAuth;
        public Params Params;

        public BasePageModel()
        {
            this.Params = new Params();
            this.PreHandler = prehandler;
            this.menuAuth = new ParamList();
            this.pageAuth = new Params();

            HttpContextAccessor hca = new HttpContextAccessor();

            this.PathName = hca.HttpContext.Request.Path.Value.ToString();

            // Ajax요청인지 여부확인
            if (hca.HttpContext.Request.Headers.ContainsKey("HS-AJAX") && hca.HttpContext.Request.Headers["HS-AJAX"].ToString().ToLower() == "true")
                this.IsAjax = true;

            string USER_ID = Cookie<User>.Store.USER_ID;
            string USER_NM = Cookie<User>.Store.USER_NM;
            string CLIENT = Cookie<User>.Store.CLIENT;

            this.Params["UserName"] = USER_NM;
            this.Params["CLIENT"] = CLIENT;
            // 여기서 선언하는 Params는 접두어로 Base 붙일것 
            this.Params["Base_HOST"] = hca.HttpContext.Request.Host.ToString();
            this.Params["Base_IsLocal"] = this.Params["Base_HOST"].AsString().StartsWith("localhost");

            // 임시처리
            this.Params["CLIENT"] = "0100";

            if (this.IsAjax == false) // 각 페이지 처음 로딩시
            {
                this.Params["R"] = false.ToString().ToLower();
                this.Params["W"] = false.ToString().ToLower();

                // 메뉴별 권한
                this.menuAuth = DTClient.GetMenuAuth();

                if (this.menuAuth != null)
                {
                    this.pageAuth = this.menuAuth.SingleOrDefault(menu => menu["S_URL"].AsString() == this.PathName);

                    if (this.pageAuth != null)
                    {
                        bool r = this.pageAuth["R"].AsBool();
                        bool w = this.pageAuth["W"].AsBool();
                        bool x = this.pageAuth["X"].AsBool();

                        if (x)
                        {
                            r = true;
                            w = true;
                        }

                        this.Params["R"] = r.ToString().ToLower();
                        this.Params["W"] = w.ToString().ToLower();
                    }
                }

                // 임시처리
                //this.Params["R"] = true.ToString().ToLower();
                //this.Params["W"] = true.ToString().ToLower();
            }
        }

        /// <summary>
        /// Get 호출
        /// </summary>
        /// <returns></returns>
        public virtual IActionResult OnGet()
        {
            // 캐시를 금지하기 위한 Cache-Control 헤더 추가
            Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");

            if (this.IsSessionRequire)
            {
                if (string.IsNullOrEmpty(Cookie<User>.Store.USER_ID) || string.IsNullOrEmpty(Cookie<User>.Store.CLIENT))
                {
                    string refPath = Request.Path + Request.QueryString;
                    return Content("<script>alert('세션이 만료되었습니다. 다시 로그인해주세요.'); top.location.href='/login';</script>", "text/html");
                }
            }

            // 로그인 세션 확인 해서 로그인 페이지로 리다이렉트 

            // 각 페이지 OnGet 처리
            if (this.OnGetHandler != null)
            {
                GetArgs args = new GetArgs();
                args.Params = new Params();

                foreach (var key in HttpContext.Request.Query.Keys)
                {
                    args.Params[key] = HttpContext.Request.Query[key].ToString();
                }

                if (args.Params.Keys.Contains("command"))
                    args.Command = args.Params["command"].AsString();

                return this.OnGetHandler(args);
            }

            return Page();
        }

        /// <summary>
        /// Post 호출
        /// </summary>
        /// <returns></returns>
        public virtual IActionResult OnPost()
        {
            // 캐시를 금지하기 위한 Cache-Control 헤더 추가
            Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");

            // 각 페이지 OnPost 처리
            if (this.OnPostHandler != null)
            {
                PostArgs args = new PostArgs();
                args.Params = new Params();
                args.Files = new List<IFormFile>();

                foreach (var key in HttpContext.Request.Form.Keys)
                {
                    (bool isJson, string type) = Util.IsJson(HttpContext.Request.Form[key].ToString());

                    if (isJson)
                    {
                        if (type == "Object")
                            args.Params[key] = new Params(HttpContext.Request.Form[key].ToString());
                        else
                            args.Params[key] = new ParamList(HttpContext.Request.Form[key].ToString());
                    }
                    else
                        args.Params[key] = HttpContext.Request.Form[key].ToString();
                }

                foreach (IFormFile file in HttpContext.Request.Form.Files)
                {
                    args.Files.Add(file);
                }

                if (args.Params.Keys.Contains("command"))
                    args.Command = args.Params["command"].AsString();

                return this.OnPostHandler(args);
            }


            return Page();
        }

        /// <summary>
        /// Post 호출 이지만 Fetch에 의한 호출(Ajax)
        /// </summary>
        /// <returns></returns>
        public virtual IActionResult OnPostFetch()
        {
            string message = "";

            // 각 페이지 OnPost 처리
            if (this.OnPostFetchHandler != null)
            {
                try
                {
                    PostArgs args = new PostArgs();
                    args.Params = new Params();
                    args.Files = new List<IFormFile>();

                    foreach (var key in HttpContext.Request.Form.Keys)
                    {
                        (bool isJson, string type) = Util.IsJson(HttpContext.Request.Form[key].ToString());

                        if (isJson)
                        {
                            if (type == "Object")
                                args.Params[key] = new Params(HttpContext.Request.Form[key].ToString());
                            else
                                args.Params[key] = new ParamList(HttpContext.Request.Form[key].ToString());
                        }
                        else
                            args.Params[key] = HttpContext.Request.Form[key].ToString();
                    }

                    foreach (IFormFile file in HttpContext.Request.Form.Files)
                    {
                        args.Files.Add(file);
                    }

                    if (args.Params.Keys.Contains("command"))
                        args.Command = args.Params["command"].AsString();

                    return this.OnPostFetchHandler(args);
                }
                catch (Exception ex)
                {
                    return new ObjectResult(new { status = "ng", message = ex.Message })
                    {
                        StatusCode = 400
                    };
                }
            }

            return new ObjectResult(new {  })
            {
                StatusCode = 200
            };
        }


        private void prehandler(PostAjaxArgs e)
        {
            // 세션 없을시 에러 처리
            if (this.IsSessionRequire)
            {
                if (string.IsNullOrEmpty(Cookie<User>.Store.USER_ID) || string.IsNullOrEmpty(Cookie<User>.Store.CLIENT))
                    throw new Exception("세션이 만료 되었습니다. 다시 로그인 해주세요.");
            }
        }
    }
}
