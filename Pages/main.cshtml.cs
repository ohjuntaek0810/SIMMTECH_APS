using DocumentFormat.OpenXml.Spreadsheet;
using HS.Core;
using HS.Web.Common;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace HS.Web.Pages
{
    public class mainModel : BasePageModel
    {
        public mainModel()
        {
            // 로그인 기능 -> true
            // 테스트용 -> false
            this.IsSessionRequire = false; 


            this.Handler = handler;
            this.OnPostHandler = OnPostPage;


            this.Params["UserName"] = Cookie.Select("USER_NM");
            //string CLIENT = Cookie<User>.Store.CLIENT;
            string USER_ID = Cookie<User>.Store.USER_ID;
            string CLIENT = "0100";
            //string USER_ID = "admin";
            this.Params["UserID"] = USER_ID;
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"
SELECT 
    A.* 
FROM
(
    SELECT 
          M.MENU_CD
        , M.UP_MENU_CD
        , M.MENU_NM
        , M.S_URL
        , M.I_CLASS
        , M.TARGET 
        , M.SORT 
        , M.MAIN_CATEGORY
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
	     END [X]
    FROM TH_GUI_MENU M
    LEFT JOIN  TH_GUI_USER_AUTH A_AUTH
        ON A_AUTH.MENU_CD = M.MENU_CD AND A_AUTH.USER_ID = '{USER_ID}'
    
    LEFT JOIN TH_GUI_USER U
	    ON U.USER_ID = '{USER_ID}'
	
    LEFT JOIN TH_GUI_GRP S
	    ON S.GRP_ID = U.LOGIN_GRP_CD

    LEFT JOIN  TH_GUI_GRP_AUTH G_AUTH
        ON G_AUTH.GRP_ID = S.GRP_ID AND G_AUTH.MENU_CD = M.MENU_CD

    WHERE 1 = 1
        AND M.CMP_CD = '{CLIENT}' 
        AND ISNULL(M.UP_MENU_CD, '') = ''  
        AND M.VIEW_YN = 'Y' 
) A
WHERE 1 = 1
AND A.R > 0
ORDER BY A.SORT
");

            // 최상우 부모
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            this.Params["HTMLTree"] = recursive(result);

            // 즐겨찾기 목록 전달
            searhFavoriteMenu();
            
        }

        private string recursive(ParamList parentList)
        {
            StringBuilder html = new StringBuilder();
            string USER_ID = Cookie<User>.Store.USER_ID;
            //string USER_ID = "admin";

            foreach (var parent in parentList)
            {
                StringBuilder sSQL = new StringBuilder();
                sSQL.Append($@"
SELECT A.* FROM
(
    SELECT 
          M.MENU_CD
        , M.UP_MENU_CD
        , M.MENU_NM
        , M.S_URL
        , M.I_CLASS
        , M.TARGET 
        , M.SORT 
        , M.MAIN_CATEGORY
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
	     END [X]
    FROM 
        TH_GUI_MENU M
    LEFT JOIN  TH_GUI_USER_AUTH A_AUTH
        ON A_AUTH.MENU_CD = M.MENU_CD AND A_AUTH.USER_ID = '{USER_ID}'
    
    LEFT JOIN TH_GUI_USER U
	    ON U.USER_ID = '{USER_ID}'
	
    LEFT JOIN TH_GUI_GRP S
	    ON S.GRP_ID = U.LOGIN_GRP_CD

    LEFT JOIN  TH_GUI_GRP_AUTH G_AUTH
        ON G_AUTH.GRP_ID = S.GRP_ID AND G_AUTH.MENU_CD = M.MENU_CD
    WHERE 
        ISNULL(M.UP_MENU_CD, '') = {parent["MENU_CD"].V} 
        AND M.VIEW_YN = 'Y' 
)A
WHERE 1 = 1
AND A.R > 0 
ORDER BY A.SORT
");

                // 최상위 부모
                ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

                if (parent["MAIN_CATEGORY"].AsString() != "")
                {
                    html.Append($@"
<li class=""nav-item menu-is-opening menu-open"" name=""{parent["MAIN_CATEGORY"].AsString()}"" style=""display:none;"">    
");
                } else
                {
                    html.Append(@"
<li class=""nav-item"">    
");
                }




                if (parent["S_URL"].AsString() == "")
                {
                    html.Append($@"
    <a href='#link' class='nav-link' >
");
                }
                else
                {
                    if (parent["TARGET"].AsString() == "")
                    {
                        html.Append($@"
    <a href='{parent["S_URL"].AsString()}' class='nav-link' data-menu-id='{parent["MENU_CD"].AsString()}' >
");
                    }
                    else
                    {
                        html.Append($@"
    <a href='{parent["S_URL"].AsString()}' class='nav-link' target='_blank' data-menu-id='{parent["MENU_CD"].AsString()}' >
");
                    }
                }

                // 클랙스 잇을시
                if (parent["I_CLASS"].AsString() != "")
                {
                    html.Append($@"
        <!-- active 메뉴 활성화 상태 -->
        <i class='{parent["I_CLASS"].AsString()}'></i>
");
                }

                html.Append($@"
        <p>
            {parent["MENU_NM"].AsString()}
");

                if (result.Count() > 0)
                {
                    html.Append($@"
            <i class=""right xi-ico xi-angle-right-min""></i>
");
                }

                html.Append($@"
        </p>
    </a>
");
                if (parent["MAIN_CATEGORY"].AsString() == "") // MAIN_CATEGORY 없을 경우 새창으로 띄우기 아이콘 넣기
                {
                    html.Append($@"
    <a href=""#link"" class=""nav-blank"" title=""새창으로"" data-window-menu='{parent["MENU_CD"].AsString()}'>
        <i class=""xi-ico xi-external-link""></i>
    </a>
");
                }

                if (result.Count > 0)
                {
                    html.Append(@"
    <ul class=""nav nav-treeview"">
");

                    string resultHTML = recursive(result);
                    html.Append($@"
        {resultHTML}
");

                    html.Append(@"
    </ul>
");
                }

                html.Append($@"                 
</li>
");
            }


            return html.ToString();
        }

        public Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "logout")
            {
                Cookie<User>.Store.Clear();
            }

            if (e.Command == "open_new_window")
            {
                Params terms = e.Params["terms"];

                Console.WriteLine("Generated SQL:");
                Console.WriteLine(terms);


                // postHandler 는 returnPage
                //toClient["data"] = this.SearchMenu(terms);
            }

            if (e.Command == "searchNotice") 
            {
                Params terms = e.Params["terms"];
                // 공지사항 목록 전달 (TOP 4개만)
                toClient["data"] = this.searchNotice(terms);
            }

            if (e.Command == "searchVOC")
            {
                Params terms = e.Params["terms"];
                // 공지사항 목록 전달 (TOP 4개만)
                toClient["data"] = this.searchVOC(terms);
            }

            if (e.Command == "viewNotice")
            {
                Params terms = e.Params["terms"];

                Params data = this.searchNotice(terms).ToParams();

                toClient["data"] = data;
            }

            if (e.Command == "viewVOC")
            {
                Params terms = e.Params["terms"];

                Params data = this.searchVOC(terms).ToParams();

                toClient["data"] = data;
            }
            // 메뉴목록 view
            if (e.Command == "view")
            {
                toClient["data"] = this.searchD();
            }
            // 바로가기 설정 저장
            if (e.Command == "save_shortcut")
            {
                Params terms = e.Params["terms"];

                this.Save(terms);
            }

            // 바로가기 설정 가져오기
            if (e.Command == "search_shortcut")
            {
                toClient["data"] = this.searchShortcut();
            }

            // 비밀번호 체크
            if (e.Command == "check_password")
            {
                Params terms = e.Params["terms"];
                terms["PASSWD"] = Encryption.SHA256Hash(terms["cur_password"].AsString());

                toClient["data"] = this.check_password(terms);
            }

            // 비밀번호 변경
            if (e.Command == "change_password")
            {
                Params terms = e.Params["terms"];

                this.change_password(terms);
            }

            return toClient;
        }

        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            Console.WriteLine("command :");
            Console.WriteLine(command);

            if (command == "open_new_window")
            {
                string menuId = e.Params["menuId"].AsString();

                Console.WriteLine("item_id:");
                Console.WriteLine(menuId);

                Params["menuId"] = menuId;
            }

            return Page();
        }

        private void searhFavoriteMenu()
        {
            StringBuilder html = new StringBuilder();

            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"
select
	SUF.USER_ID,
	SUF.MENU_ID,
	SM.MENU_NM
from
	TH_GUI_USER_FAVORITES SUF
	INNER JOIN TH_GUI_MENU SM ON SUF.MENU_ID = SM.MENU_CD
");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            // TODO : data-set 으로 클릭 이벤트 만들어줘야함.--> 
            foreach (var item in result)
            {
                html.Append($@"<li class=""nav-item"">
    <a href=""#link"" class=""nav-link"">
        <p>
            {item["MENU_NM"].AsString()}
        </p>
    </a>
    <a href=""#link"" class=""nav-blank"" title=""새창으로"">
        <i class=""xi-ico xi-external-link""></i>
    </a>
</li>
");
            }

            Params["FavoriteTree"] = html.ToString();

        }

        // 공지사항 검색
        private DataTable searchNotice(Params terms)
        {

            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"
SELECT TOP 4
    SEQ,
	TITLE,
	DESCRIPTION,
	CASE
		WHEN UPDATE_ID IS NOT NULL THEN UPDATE_ID
		ELSE INSERT_ID 
	END AS INSERT_ID,
	CASE
		WHEN UPDATE_DTTM IS NOT NULL THEN FORMAT(UPDATE_DTTM, 'yyyy-MM-dd') 
		ELSE FORMAT(INSERT_DTTM, 'yyyy-MM-dd') 
	END AS INSERT_DTTM
FROM
	[dbo].[TH_GUI_NOTICE]
WHERE
	1=1
	AND DEL_YN = 'N'
    AND USE_YN = 'Y'
");
            if (terms["SEQ"].Length > 0)
            {
                sSQL.Append($@"
    AND SEQ = {terms["SEQ"].AsString()}
");
            }

                return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable searchVOC(Params terms)
        {

            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"
SELECT TOP 4
    SEQ,
	TITLE,
	DESCRIPTION,
	CASE
		WHEN UPDATE_ID IS NOT NULL THEN UPDATE_ID
		ELSE INSERT_ID 
	END AS INSERT_ID,
	CASE
		WHEN UPDATE_DTTM IS NOT NULL THEN FORMAT(UPDATE_DTTM, 'yyyy-MM-dd') 
		ELSE FORMAT(INSERT_DTTM, 'yyyy-MM-dd') 
	END AS INSERT_DTTM
FROM
	[dbo].[TH_GUI_VOC]
WHERE
	1=1
	AND DEL_YN = 'N'
");
            if (terms["SEQ"].Length > 0)
            {
                sSQL.Append($@"
    AND SEQ = {terms["SEQ"].AsString()}
");
            }

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// 조희로직
        /// </summary>
        /// <param name="terms"></param>
        /// <return></return>
        private DataTable searchD()
        {
            string USER_ID = Cookie<User>.Store.USER_ID;

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT A.* FROM
(
    SELECT 
          M.MENU_CD
        , M.UP_MENU_CD
        , M.MENU_NM
        , M.S_URL
        , M.I_CLASS
        , M.TARGET 
        , M.SORT 
        , M.MAIN_CATEGORY
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
	     END [X]
    FROM 
        TH_GUI_MENU M
    LEFT JOIN  TH_GUI_USER_AUTH A_AUTH
        ON A_AUTH.MENU_CD = M.MENU_CD AND A_AUTH.USER_ID = '{USER_ID}'
    LEFT JOIN TH_GUI_USER U
	    ON U.USER_ID = '{USER_ID}'
    LEFT JOIN TH_GUI_GRP S
	    ON S.GRP_ID = U.LOGIN_GRP_CD
    LEFT JOIN  TH_GUI_GRP_AUTH G_AUTH
        ON G_AUTH.GRP_ID = S.GRP_ID AND G_AUTH.MENU_CD = M.MENU_CD
    WHERE 
        M.VIEW_YN = 'Y' 
)A
WHERE 1 = 1
AND A.R > 0 
ORDER BY A.SORT
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable searchShortcut()
        {
            string USER_ID = Cookie<User>.Store.USER_ID;

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
	A.SEQ,
	A.USER_ID,
	A.MENU_CD,
	B.MENU_NM,
	CASE 
		WHEN SUBSTRING(A.MENU_CD, 1, 2) = 'MD' THEN 'Master Data'
		WHEN SUBSTRING(A.MENU_CD, 1, 2) = 'PC' THEN 'Plan Control'
		WHEN SUBSTRING(A.MENU_CD, 1, 2) = 'PA' THEN 'Plan Analaysis'
		WHEN SUBSTRING(A.MENU_CD, 1, 2) = 'OP' THEN 'Order Promising'
		WHEN SUBSTRING(A.MENU_CD, 1, 2) = 'si' THEN 'Admin'
		ELSE 'Etc.'
	END AS UPPER_MENU_NM,
	A.I_CLASS,
    B.S_URL,
	A.ORDER_SEQ
FROM
	TH_GUI_USER_SHORTCUT A WITH (NOLOCK)
	INNER JOIN TH_GUI_MENU B WITH (NOLOCK) ON A.MENU_CD = B.MENU_CD 
WHERE
    1=1
    AND USER_ID = '{USER_ID}'
");


            sSQL.Append($@"
ORDER BY A.ORDER_SEQ
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable check_password(Params terms)
        {
            string USER_ID = Cookie<User>.Store.USER_ID;

            StringBuilder sSQL = new StringBuilder();
            
            sSQL.Append($@"
SELECT 
	*
FROM
	TH_GUI_USER
WHERE
	1=1
	AND USER_ID = '{USER_ID}'
	AND PASSWD = {terms["PASSWD"].V}
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private void change_password(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

            string USER_ID = Cookie<User>.Store.USER_ID;

            // 비밀번호 SHA256 암호화
            terms["PASSWD"] = Encryption.SHA256Hash(terms["chg_password"].AsString());

            sSQL.Append($@"
UPDATE [dbo].[TH_GUI_USER] SET PASSWD = {terms["PASSWD"].V} WHERE USER_ID = '{USER_ID}'
");

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            HS.Web.Proc.TH_GUI_USER_SHORTCUT.Save(data);
        }

    }
}
