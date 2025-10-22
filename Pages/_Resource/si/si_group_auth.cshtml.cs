using HS.Core;
using HS.Web.Common;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class si_group_auth : BasePageModel
    {   
        public si_group_auth()
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
            
            if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                //Params data = this.searchD(terms);

                toClient["data"] = this.searchD(terms);
            }

            if (e.Command == "save")
            {
                Params terms = e.Params["terms"];
                ParamList data = e.Params["data"];

                data.ForEach(d =>
                {
                    d["USER_ID"] = terms["USER_ID"];
                    if (d["R"] == true)
                    {
                        d["R"] = 1;
                    }
                    else
                    {
                        d["R"] = 0;
                    }
                    if (d["W"] == true)
                    {
                        d["W"] = 2;
                    }
                    else
                    {
                        d["W"] = 0;
                    }
                    if (d["X"] == true)
                    {
                        d["X"] = 4;
                    }
                    else
                    {
                        d["X"] = 0;
                    }

                    d["GRP_AUTH"] = d["R"].AsNum() + d["W"].AsNum() + d["X"].AsNum();
                });

                // 데이터 저장
                this.Save(data);
            }

            if (e.Command == "delete")
            {
                ParamList data = e.Params["data"];


                this.delete(data);
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
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
SELECT
	SG.GRP_ID 
	, SG.GRP_NM 
	, SG.RMK
	, SG.USE_YN
FROM
	TH_GUI_GRP SG
WHERE 1 = 1
AND SG.CMP_CD = " + this.Params["CLIENT"].V + @"
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
        (SG.GRP_ID LIKE '%{search}%') OR
        (SG.GRP_NM LIKE '%{search}%') 
    )
");
                    }
                    else
                    {
                        sSQL.Append($@"
    OR 
    (
        (SG.GRP_ID LIKE '%{search}%') OR
        (SG.GRP_NM LIKE '%{search}%') 
    )
");
                    }

                    index++;
                });

                sSQL.Append(@"
)
");
            }

            if (terms["GRP_ID"].Length > 0)
            {
                sSQL.Append($@"
AND SG.GRP_ID = {terms["GRP_ID"].V}
");
                return Data.Get(sSQL.ToString()).Tables[0];
            }
            sSQL.Append(@"
ORDER BY SG.GRP_ID
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// 조희로직
        /// </summary>
        /// <param name="terms"></param>
        /// <return></return>
        private DataTable searchD(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
    sg.GRP_ID ,
	sm.SORT ,
	sm.CMP_CD, 
	sm.UP_MENU_CD,      
	sm.MENU_CD,      
	sm.MENU_NM ,     
	/*
	, TRUE AS R
	, TRUE AS W
	, TRUE AS X
	*/
	CASE WHEN sga.GRP_AUTH IN ('1','3','5','7') THEN 1 ELSE 0 END AS R,
	CASE WHEN sga.GRP_AUTH IN ('2','3','6','7') THEN 1 ELSE 0 END AS W,
	CASE WHEN sga.GRP_AUTH IN ('4','5','6','7') THEN 1 ELSE 0 END AS X
FROM TH_GUI_MENU AS sm
LEFT JOIN TH_GUI_GRP AS sg
    ON sg.GRP_ID = {terms["GRP_ID"].V}
LEFT JOIN  TH_GUI_GRP_AUTH AS sga
    ON sga.GRP_ID = sg.GRP_ID AND sga.MENU_CD = sm.MENU_CD
WHERE 1 = 1
    AND sm.CMP_CD = '0100' 
    AND sm.USE_YN = 'Y'
ORDER BY sm.SORT
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }














        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList dataList)
        {
            HS.Web.Proc.TH_GUI_GRP_AUTH.Save(dataList);
        }


        /// <summary>
        /// 선택한 항목 삭제
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            StringBuilder sSQL = new StringBuilder();

            data.ForEach(D =>
            {
                sSQL.AppendLine($"DELETE FROM TH_GUI_GRP WHERE CMP_CD = {D["CLIENT"].V} AND GRP_ID = {D["GRP_ID"].V};");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }
    }
}
