using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace HS.Web.Controllers
{
    [ApiController]
    [Route("api/file/")]
    public class FileController : BaseController
    {

        [HttpPost]
        [Route("upload")]
        [RequestSizeLimit(1_000_000_000)]
        public async Task<IActionResult> upload(IFormFile file) // , [FromForm] string CLIENT = null 인자 string으로 받을때
        {
            if (file == null || file.Length == 0)
                return BadRequest("File does not exist.");

            try
            {
                // 파일 저장 경로 설정
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "DT\\Video", file.FileName);

                // 디렉토리가 없으면 생성
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // 파일 저장
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok("File upload completed.");
            }
            catch (Exception ex)
            {
                string err = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] : {ex.Message}{Environment.NewLine}";
                err += $"{ex.StackTrace}{Environment.NewLine}";
                System.IO.File.AppendAllText("D:/fileupload_error.txt", err);


                return new ObjectResult(new { 
                    status = "ng", message = ex.Message
                }) { StatusCode = 400 };
            }
        }

        [EnableCors("apipoilcy")]
        [HttpGet]
        [HttpPost]
        [Route("down")]
        public IActionResult down() 
        {
            try
            {
                string fileguid = this.Params["fileguid"].AsString();

                // 디비에서 파일정보조회
                Params fileResult = Data.Get($@"
SELECT 
	FILE_ORG_NAME
	, FILE_PATH
	, FILE_MIME_TYPE
	, FILE_EXT
FROM SI_FILE_INFO WHERE FILE_GUID = '{fileguid}'").Tables[0].ToParams();

                if (fileResult != null)
                {
                    string FILE_ORG_NAME = fileResult["FILE_ORG_NAME"].AsString();
                    string FILE_PATH = fileResult["FILE_PATH"].AsString();
                    string FILE_MIMI_TYPE = fileResult["FILE_MIME_TYPE"].AsString();
                    // 파일 저장 경로 설정
                    var filePath = Directory.GetCurrentDirectory() + FILE_PATH + FILE_ORG_NAME;

                    return new PhysicalFileResult(filePath, FILE_MIMI_TYPE);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return new ObjectResult(new
                {
                    status = "ng",
                    message = ex.Message
                })
                { StatusCode = 400 };
            }
        }
    }
}
