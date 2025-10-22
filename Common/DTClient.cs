using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Text;

namespace HS.Web.Common
{
    public class DTClient
    {
        public static bool Login(IHttpContextAccessor acc, Params logindata)
        {
            // 디비 갔다와서
            string sSQL = $@"SELECT * FROM TH_GUI_USER WHERE CMP_CD = {logindata["CLIENT"].V} AND USER_ID = {logindata["USER_ID"].V} AND PASSWD = {logindata["PASSWD"].V} ;";
            DataTable result = Data.Get(sSQL.ToString()).Tables[0];

            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            bool isLocal = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

            if (result.Rows.Count > 0)
            {
                if (logindata["chk-id-keep"].AsBool() == true)
                {
                    Cookie.Append("UID", result.Rows[0]["USER_ID"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Month });
                    //Cookie.Append("CLIENT", result.Rows[0]["CLIENT"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Month });
                    Cookie.Append("chk-id-keep", "true", new CookieOption { Timeout = 3, Interval = CookieInerval.Month });
                }
                else
                {
                    Cookie.Delete("UID");
                    //Cookie.Delete("CLIENT");
                    Cookie.Delete("chk-id-keep");
                }

                Cookie.Append("USER_NM", result.Rows[0]["USER_NM"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour, Domain = Variable.Domain });
                Cookie.Append("LOGIN_CI", Cookie.Select("_CI"), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour, Domain = Variable.Domain });

                //if (isLocal)
                //{
                //    //Cookie.Append("DT_CLIENT", result.Rows[0]["CLIENT"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour });
                //    //Cookie.Append("DT_USER_ID", result.Rows[0]["USER_ID"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour });
                //    //Cookie.Append("DT_USER_NM", result.Rows[0]["USER_NM"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour });
                //}
                //else
                //{
                //    //string domain = "." + acc.HttpContext.Request.Host.Host;
                //    string domain = "." + "owon.kr";

                //    //if (logindata["USER_ID"].AsString() == "testuser100")
                //    //{
                //    //    Cookie.Append("DT_CLIENT", result.Rows[0]["CLIENT"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Month, Domain = domain });
                //    //    Cookie.Append("DT_USER_ID", result.Rows[0]["USER_ID"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Month, Domain = domain });
                //    //    Cookie.Append("DT_USER_NM", result.Rows[0]["USER_NM"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Month, Domain = domain });
                //    //}
                //    //else
                //    //{
                //    //    Cookie.Append("DT_CLIENT", result.Rows[0]["CLIENT"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour, Domain = domain });
                //    //    Cookie.Append("DT_USER_ID", result.Rows[0]["USER_ID"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour, Domain = domain });
                //    //    Cookie.Append("DT_USER_NM", result.Rows[0]["USER_NM"].ToString(), new CookieOption { Timeout = 3, Interval = CookieInerval.Hour, Domain = domain });
                //    //}
                //}


                Cookie<User>.Store.CLIENT = result.Rows[0]["CMP_CD"].ToString();
                Cookie<User>.Store.USER_ID = result.Rows[0]["USER_ID"].ToString();
                Cookie<User>.Store.USER_NM = result.Rows[0]["USER_NM"].ToString();


                //// 로그인 로그
                //Params loginLog = new Params();
                //loginLog["LOG_ID"] = logindata["USER_ID"];
                //loginLog["LOG_DT"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //loginLog["LOG_IP"] = acc.HttpContext.Connection.RemoteIpAddress.ToString();
                //loginLog["LOG_PORT"] = acc.HttpContext.Connection.RemotePort.ToString();
                //loginLog["LOG_PATH"] = acc.HttpContext.Request.Path.Value.ToString();

                //SI_LOGIN_LOG.Save(loginLog);

                

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void Logout()
        {
            string CLIENT = Cookie<User>.Store.CLIENT;
            string USER_ID = Cookie<User>.Store.USER_ID;
            string cacheID = $"{CLIENT}_{USER_ID}";

            Cookie.Delete("USER_NM");
            Cookie.Delete("LOGIN_CI");
            Cookie<User>.Store.Clear();
            CacheManager.DelCache(cacheID);
        }

        public static Params UserInfo(Params data)
        {
            Params result = new Params();

            result["CLIENT"] = "0100";
            //result["CLIENT"] = data["CLIENT"].AsString() == "" ? Cookie<User>.Store.CLIENT : data["CLIENT"];
            result["USER_ID"] = data["USER_ID"].AsString() == "" ? Cookie<User>.Store.USER_ID : data["USER_ID"];
            result["USER_NM"] = data["USER_NM"].AsString() == "" ? Cookie<User>.Store.USER_NM : data["USER_NM"];

            //result["USER_ID"] = data["USER_ID"].AsString() == "" ? "admin" : data["USER_ID"];
            //result["USER_NM"] = data["USER_NM"].AsString() == "" ? "관리자" : data["USER_NM"];

            return result;
        }

        public static void UserInfoMerge(Params data)
        {
            data.Merge(DTClient.UserInfo(data));
        }

        public static ParamList GetMenuAuth()
        {
            StringBuilder sSQL = new StringBuilder();

            string CLIENT = Cookie<User>.Store.CLIENT;
            string USER_ID = Cookie<User>.Store.USER_ID;

            sSQL.Append($@"
SELECT 
      M.MENU_CD
    , M.S_URL
    , CASE WHEN A_AUTH.USER_AUTH IS NOT NULL 
        THEN 
		(CASE WHEN A_AUTH.USER_AUTH = '1' OR A_AUTH.USER_AUTH = '3' OR A_AUTH.USER_AUTH = '5' OR A_AUTH.USER_AUTH = '7' THEN 1 ELSE 0 END )
	    ELSE 
		(CASE WHEN G_AUTH.GRP_AUTH = '1' OR G_AUTH.GRP_AUTH = '3' OR G_AUTH.GRP_AUTH = '5' OR G_AUTH.GRP_AUTH = '7' THEN 1 ELSE 0 END )
	    END [R]
    , CASE WHEN A_AUTH.USER_AUTH IS NOT NULL 
        THEN 
		(CASE WHEN A_AUTH.USER_AUTH = '2' OR A_AUTH.USER_AUTH = '3' OR A_AUTH.USER_AUTH = '6' OR A_AUTH.USER_AUTH = '7' THEN 1 ELSE 0 END )
	    ELSE 
		(CASE WHEN G_AUTH.GRP_AUTH = '2' OR G_AUTH.GRP_AUTH = '3' OR G_AUTH.GRP_AUTH = '6' OR G_AUTH.GRP_AUTH = '7' THEN 1 ELSE 0 END )
	    END [W]
    , CASE WHEN A_AUTH.USER_AUTH IS NOT NULL 
        THEN 
		(CASE WHEN A_AUTH.USER_AUTH = '4' OR A_AUTH.USER_AUTH = '5' OR A_AUTH.USER_AUTH = '6' OR A_AUTH.USER_AUTH = '7' THEN 1 ELSE 0 END )
	    ELSE 
		(CASE WHEN G_AUTH.GRP_AUTH = '4' OR G_AUTH.GRP_AUTH = '5' OR G_AUTH.GRP_AUTH = '6' OR G_AUTH.GRP_AUTH = '7' THEN 1 ELSE 0 END )
	    END AS [X]

FROM TH_GUI_MENU M

LEFT JOIN TH_GUI_USER_AUTH  A_AUTH  ON A_AUTH.MENU_CD = M.MENU_CD AND A_AUTH.USER_ID = '{USER_ID}'
    
LEFT JOIN TH_GUI_USER       U       ON U.USER_ID = '{USER_ID}'
	
LEFT JOIN TH_GUI_GRP        S       ON S.GRP_ID = U.LOGIN_GRP_CD

LEFT JOIN TH_GUI_GRP_AUTH   G_AUTH  ON G_AUTH.GRP_ID = S.GRP_ID AND G_AUTH.MENU_CD = M.MENU_CD

WHERE 1 = 1
    AND M.CMP_CD = '{CLIENT}' 
    AND M.VIEW_YN = 'Y' 
");

            return Data.Get(sSQL.ToString()).Tables[0].ToParamList();
        }
    }
}
