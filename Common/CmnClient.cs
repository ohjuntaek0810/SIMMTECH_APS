using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Text;

namespace HS.Web.Common
{
    public class CmnClient
    {
        /// <summary>
        /// 즐겨찾기 확인
        /// </summary>
        public static DataTable checkFavorite(Params data)
        {
            //string CLIENT = Cookie<User>.Store.CLIENT;
            //string USER_ID = Cookie<User>.Store.USER_ID;
            string CLIENT = "0100";
            string USER_ID = "admin";

            string MENU_ID = data["curMenuId"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT 
	1
FROM 
	TH_GUI_USER_FAVORITES SUF
WHERE
	1=1
	AND SUF.USER_ID = '{USER_ID}'
	AND SUF.MENU_ID = '{MENU_ID}'
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 즐겨찾기 추가
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void addFavorite(Params data)
        {
            //string CLIENT = Cookie<User>.Store.CLIENT;
            string USER_ID = Cookie<User>.Store.USER_ID;
            string CLIENT = "0100";

            string MENU_ID = data["curMenuId"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
INSERT INTO TH_GUI_USER_FAVORITES (USER_ID, MENU_ID)
VALUES ('{USER_ID}', '{MENU_ID}')
");
            HS.Web.Common.Data.Execute(sSQL.ToString());
        }

        /// <summary>
        /// 즐겨찾기 해제
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static void deleteFavorite(Params data)
        {
            //string CLIENT = Cookie<User>.Store.CLIENT;
            string USER_ID = Cookie<User>.Store.USER_ID;
            string CLIENT = "0100";

            string MENU_ID = data["curMenuId"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
DELETE FROM TH_GUI_USER_FAVORITES
WHERE USER_ID = '{USER_ID}' AND MENU_ID = '{MENU_ID}';
");
            HS.Web.Common.Data.Execute(sSQL.ToString());
        }


        // ========================================================================================================
        // GRID 개인화 관련
        /// <summary>
        /// 저장된 그리드 헤더컬럼 가져오기
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static DataTable SearchGrid(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            string USER_ID = Cookie<User>.Store.USER_ID;
            string GRID_ID = terms["grid_id"].AsString();

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
	SUG.DATA_FIELD AS dataField,
    SUG.COLUMN_NAME AS label,
	SUG.VISIBLE AS visible,
	SUG.WIDTH AS width,
	SUG.FIX AS fixed,
	SUG.EDITABLE AS editable
FROM
	TH_GUI_USER_GRID SUG
WHERE
	1=1
	AND SUG.USER_ID = '{USER_ID}'
	AND SUG.GRID_ID = '{GRID_ID}'
ORDER BY SUG.COLUMN_ORDER
");

            return Data.Get(sSQL.ToString()).Tables[0];

        }

        /// <summary>
        /// 그리드 헤더컬럼 옵션 저장
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void SaveGrid(ParamList dataList)
        {
            HS.Web.Proc.SI_GRID.Save(dataList);
        }
    }
}
