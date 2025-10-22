using HS.Core;
using HS.Web.Common;
using System.Net;
using System.Net.Mail;

namespace HS.Web.Pages
{
    public class MailModel : BasePageModel
    {
        public MailModel()
        {
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs arg)
        {
            Params result = new Params();

            if (arg.Command == "mail")
            {
                string senderEmail = "hssoft2000@gmail.com"; // 송신자 Gmail 계정
                string senderPassword = "oasv iwar kmtl sxka"; // 송신자 Gmail 계정의 앱 비밀번호

                string recipientEmail = "bongsoo@hansolsoft.com"; // 수신자 이메일 주소

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(senderEmail);
                mailMessage.To.Add(recipientEmail);
                mailMessage.Subject = "테스트 제목";
                mailMessage.Body = "테스트 본문";

                using (SmtpClient smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587; // Gmail SMTP 포트
                    //smtpClient.Port = 465; // Gmail SMTP 포트
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtpClient.EnableSsl = true;

                    try
                    {
                        smtpClient.Send(mailMessage);
                        result["msg"] = "발신성공";
                    }
                    catch (Exception ex)
                    {
                        result["msg"] = $"발신실패 : {ex.Message}";
                    }
                }
            }

            return result;
        }
    }
}
