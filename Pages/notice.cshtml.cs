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
                vali.Null("TITLE", "공지사항 제목이 입력되지 않았습니다.");
                vali.Null("DESCRIPTION", "공지사항 내용이 입력되지 않았습니다.");
                vali.Null("USE_YN", "사용 여부가 입력되지 않았습니다.");

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
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            HS.Web.Proc.TH_GUI_NOTICE.Save(data);
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

                // 안쓰는거 왜 넘기는지..?
                Params terms = e.Params["terms"];
                ParamList list = e.Params["list"];

                // TODO : 서버로 파일 넘겨지는지 확인필요. 

                // 파일 저장 경로 설정 (현재웹서버 경로의 하위 디렉토리
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\files", file.FileName); // wwwroot하위로 저장하면 url경로로 접근이 가능(보안에 취약)
                //var filePath = Path.Combine(Directory.GetCurrentDirectory(), "files", file.FileName); // 내부경로로 저장시 url경로로 접근 불가능
                //var filePath = "F://5/" + file.FileName; // 특정경로로 지정시  


                // 디렉토리가 없으면 생성 check
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // 파일 저장
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // 해당 파일정보로 디비 저장
                FileInfo fileInfo = new FileInfo(filePath);
                string fileName = fileInfo.Name;
                long fileLength = fileInfo.Length / 1024; // kbyte 단위 
                string fileExtenseion = fileInfo.Extension;
                string fileMimeType = HS.Core.File.GetMimeType(fileName);


                // 파일 업로드 했다 가정하고
                returnMessage = "파일 업로드가 완료 되었습니다.";
            }


            return new ObjectResult(new { sttaus = "ok", message = returnMessage })
            {
                StatusCode = 200
            };
        }
    }
}
