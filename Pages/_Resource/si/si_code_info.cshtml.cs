using HS.Core;
using HS.Web.Common;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class si_code_info : BasePageModel
    {   
        public si_code_info()
        {
            //this.IsSessionRequire = false;
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            if (e.Command == "save")
            {
                Params data = e.Params["data"];

                #region 유효성 검사
                Vali vali = new Vali(data);
                vali.Null("GRP_CD", "코드그룹이 입력되지 않았습니다.");
                vali.Null("CMN_CD", "공통코드가 입력되지 않았습니다.");
                vali.Null("CMN_NM", "공통코드명이 입력되지 않았습니다.");

                vali.DoneDeco();
                #endregion

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
        private DataTable Search(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT 
	 A.CMP_CD	
	,A.GRP_CD
	,B.GRP_NM
	,A.CMN_CD
	,A.CMN_NM
	,A.UP_CMN_CD
    ,C.CMN_NM       AS UP_CMN_NM
	,A.SEQ
	,A.USE_YN
	,A.RMK
FROM SI_CODE_INFO A
LEFT JOIN SI_CODE_GROUP B ON B.CMP_CD = A.CMP_CD AND B.GRP_CD = A.GRP_CD
LEFT JOIN SI_CODE_INFO  C ON C.CMP_CD = A.CMP_CD AND C.CMN_CD = A.UP_CMN_CD
WHERE 1 = 1
AND A.CMP_CD = {terms["CLIENT"].V}
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
        (A.CMN_CD LIKE '%{search}%') OR
        (A.CMN_NM LIKE '%{search}%') OR
        (B.GRP_NM LIKE '%{search}%')
    )
");
                    }
                    else
                    {
                        sSQL.Append($@"
    OR 
    (
        (A.CMN_CD LIKE '%{search}%') OR
        (A.CMN_NM LIKE '%{search}%') OR
        (B.GRP_NM LIKE '%{search}%')
    )
");
                    }

                    index++;
                });

                sSQL.Append(@"
)
");
            }

            if (terms["CMN_CD"].Length > 0 && terms["GRP_CD"].Length > 0)
            {
                sSQL.AppendLine($@"
AND A.CMN_CD = {terms["CMN_CD"].V}
AND A.GRP_CD = {terms["GRP_CD"].V}

");
                return Data.Get(sSQL.ToString()).Tables[0];
            }
            sSQL.Append(@"
ORDER BY A.GRP_CD, A.SEQ
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
            HS.Web.Proc.SI_CODE_INFO.Save(data);
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
                sSQL.Append($@"
DELETE FROM SI_COMMON_CODE WHERE CLIENT = {D["CLIENT"].V} AND COMMON_CD = {D["COMMON_CD"].V};
");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }
    }
}
