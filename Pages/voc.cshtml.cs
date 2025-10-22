using HS.Core;
using HS.Web.Common;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class voc : BasePageModel
    {   
        public voc()
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

                Params data = this.search(terms).ToParams();

                toClient["data"] = data;
            }

            if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("TITLE", "공지사항 제목이 입력되지 않았습니다.");
                vali.Null("DESCRIPTION", "공지사항 내용이 입력되지 않았습니다.");

                vali.DoneDeco();

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
    SEQ,
	TITLE,
	DESCRIPTION,
	INSERT_ID,
	INSERT_DTTM,
	UPDATE_ID,
	UPDATE_DTTM
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
        (INSERT_ID LIKE '%{search}%') OR
        (TITLE LIKE '%{search}%') 
    )
");
                    }
                    else
                    {
                        sSQL.Append($@"
    OR 
    (
        (INSERT_ID LIKE '%{search}%') OR
        (TITLE LIKE '%{search}%') 
    )
");
                    }

                    index++;
                });

                sSQL.Append(@"
)
");
            }

            sSQL.Append(@"
ORDER BY UPDATE_DTTM DESC
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
            HS.Web.Proc.TH_GUI_VOC.Save(data);
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
                sSQL.AppendLine($"UPDATE TH_GUI_VOC SET DEL_YN = 'Y' WHERE SEQ = {D["SEQ"].D}");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }
    }
}
