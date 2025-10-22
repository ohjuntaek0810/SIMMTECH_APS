using HS.Core;
using HS.Web.Common;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class TH_GUI_USER : BasePageModel
    {   
        public TH_GUI_USER()
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
                data["HID_PASSWD"] = data["PASSWD"];

                if (data["PASSWD"].AsString().Length == 64)
                {
                    data["PASSWD"] = string.Empty;
                }

                toClient["data"] = data;
            }

            if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("USER_ID", "ID�� �Էµ��� �ʾҽ��ϴ�.");

                if (string.IsNullOrEmpty(data["HID_PASSWD"].AsString()))
                {
                    vali.Null("PASSWD", "�н����� �Էµ��� �ʾҽ��ϴ�.");

                    if (!string.IsNullOrEmpty(data["PASSWD"].AsString()))
                    {
                        vali.Custom(C => {

                            string password = C["PASSWD"].AsString();

                            if (password.Length < 4)
                                return true;

                            // ���ڸ� �����ϴ��� Ȯ��
                            //if (!password.Any(char.IsLetter))
                            //    return true;

                            // Ư�����ڸ� �����ϴ��� Ȯ��
                            //if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                            //    return true;

                            return false;
                        }, "�н������ 4�ڸ� �̻� ������ �Ǿ�� �մϴ�.");
                    }
                }
                else
                {
                    if(!string.IsNullOrEmpty(data["PASSWD"].AsString()))
                    {
                        vali.Custom(C => {

                            string password = C["PASSWD"].AsString();

                            if (password.Length < 8)
                                return true;

                            // ���ڸ� �����ϴ��� Ȯ��
                            if (!password.Any(char.IsLetter))
                                return true;

                            // Ư�����ڸ� �����ϴ��� Ȯ��
                            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                                return true;

                            return false;
                        }, "�н������ 8�ڸ� �̻� ����, Ư�����ڰ� ���ԵǾ�� �մϴ�.");
                    }
                }

                vali.DoneDeco();

                // ��й�ȣ SHA256 ��ȣȭ
                if (!string.IsNullOrEmpty(data["PASSWD"].AsString()))
                    data["PASSWD"] = Encryption.SHA256Hash(data["PASSWD"].AsString());
                else
                    data["PASSWD"] = data["HID_PASSWD"];

                // ������ ����
                this.Save(data);
            }

            if (e.Command == "delete")
            {
                ParamList data = e.Params["data"];


                this.delete(data);
            }

            if (e.Command == "encrypt")
            {
                Params terms = e.Params["terms"];
                ParamList data = e.Params["data"];

                data.ForEach(D => { D["PASSWD"] = Encryption.SHA256Hash(D["PASSWD"].AsString()); });

                this.Save(data);

                toClient["data"] = this.search(terms);
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
      A.CMP_CD
    , A.USER_ID       
    , A.USER_NM       
    , A.PASSWD        
    , A.LOGIN_GRP_CD  
    , GRP.GRP_NM AS LOGIN_GRP_NM    
    , A.RMK  
    , A.USE_YN        
    , A.REG_DM        
    , A.REG_ID        
    , A.MDF_DM        
    , A.MDF_ID        
FROM
     TH_GUI_USER A
LEFT JOIN TH_GUI_GRP GRP ON GRP.GRP_ID = A.LOGIN_GRP_CD
WHERE 1 = 1
AND A.CMP_CD = " + this.Params["CLIENT"].V + @"
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
        (A.USER_ID LIKE '%{search}%') OR
        (A.USER_NM LIKE '%{search}%') 
    )
");
                    }
                    else
                    {
                        sSQL.Append($@"
    OR 
    (
        (A.USER_ID LIKE '%{search}%') OR
        (A.USER_NM LIKE '%{search}%') 
    )
");
                    }

                    index++;
                });

                sSQL.Append(@"
)
");
            }

            if (terms["USER_ID"].Length > 0)
            {
                sSQL.Append($@"
AND A.USER_ID = {terms["USER_ID"].V}
");
                return Data.Get(sSQL.ToString()).Tables[0];
            }
            sSQL.Append(@"
ORDER BY A.USER_ID
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }


        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            HS.Web.Proc.TH_GUI_USER.Save(data);
        }


        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            HS.Web.Proc.TH_GUI_USER.Save(data);
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
                sSQL.AppendLine($"DELETE FROM TH_GUI_USER WHERE CLIENT = {D["CLIENT"].V} AND USER_ID = {D["USER_ID"].V};");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }
    }
}
