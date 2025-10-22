using HS.Core;
using HS.Web.Common;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class si_group : BasePageModel
    {   
        public si_group()
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
                vali.Null("GRP_ID", "�׷� �ڵ尡 �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("GRP_NM", "�׷���� �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("USE_YN", "���̱� ���ΰ� �Էµ��� �ʾҽ��ϴ�.");

                vali.DoneDeco();

                // ������ ����
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
        /// ��ȸ ���� 
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

        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            HS.Web.Proc.TH_GUI_GRP.Save(data);
        }


        /// <summary>
        /// ������ �׸� ����
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
