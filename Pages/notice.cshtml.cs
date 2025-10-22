using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class notice : BasePageModel
    {   
        public notice()
        {
            this.Handler = handler;
            this.OnPostFetchHandler = OnPostFetchPage;
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
                vali.Null("TITLE", "�������� ������ �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("DESCRIPTION", "�������� ������ �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("USE_YN", "��� ���ΰ� �Էµ��� �ʾҽ��ϴ�.");

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
    SEQ,
	TITLE,
	DESCRIPTION,
	USE_YN,
	INSERT_ID,
	INSERT_DTTM,
	UPDATE_ID,
	UPDATE_DTTM
FROM
	[dbo].[TH_GUI_NOTICE]
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
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            HS.Web.Proc.TH_GUI_NOTICE.Save(data);
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
                sSQL.AppendLine($"UPDATE TH_GUI_NOTICE SET DEL_YN = 'Y' WHERE SEQ = {D["SEQ"].D}");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }
        private IActionResult OnPostFetchPage(PostArgs e)
        {
            string returnMessage = "";

            if (e.Command == "fileUplaod")
            {
                IFormFile file = e.Files[0];

                // �Ⱦ��°� �� �ѱ����..?
                Params terms = e.Params["terms"];
                ParamList list = e.Params["list"];

                // TODO : ������ ���� �Ѱ������� Ȯ���ʿ�. 

                // ���� ���� ��� ���� (���������� ����� ���� ���丮
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\files", file.FileName); // wwwroot������ �����ϸ� url��η� ������ ����(���ȿ� ���)
                //var filePath = Path.Combine(Directory.GetCurrentDirectory(), "files", file.FileName); // ���ΰ�η� ����� url��η� ���� �Ұ���
                //var filePath = "F://5/" + file.FileName; // Ư����η� ������  


                // ���丮�� ������ ���� check
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // ���� ����
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // �ش� ���������� ��� ����
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;
                long fileLength = fileInfo.Length / 1024; // kbyte ���� 
                string fileExtenseion = fileInfo.Extension;
                string fileMimeType = HS.Core.File.GetMimeType(fileName);


                // ���� ���ε� �ߴ� �����ϰ�
                returnMessage = "���� ���ε尡 �Ϸ� �Ǿ����ϴ�.";
            }


            return new ObjectResult(new { sttaus = "ok", message = returnMessage })
            {
                StatusCode = 200
            };
        }
    }
}
