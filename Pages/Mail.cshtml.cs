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
                string senderEmail = "hssoft2000@gmail.com"; // �۽��� Gmail ����
                string senderPassword = "oasv iwar kmtl sxka"; // �۽��� Gmail ������ �� ��й�ȣ

                string recipientEmail = "bongsoo@hansolsoft.com"; // ������ �̸��� �ּ�

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(senderEmail);
                mailMessage.To.Add(recipientEmail);
                mailMessage.Subject = "�׽�Ʈ ����";
                mailMessage.Body = "�׽�Ʈ ����";

                using (SmtpClient smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587; // Gmail SMTP ��Ʈ
                    //smtpClient.Port = 465; // Gmail SMTP ��Ʈ
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtpClient.EnableSsl = true;

                    try
                    {
                        smtpClient.Send(mailMessage);
                        result["msg"] = "�߽ż���";
                    }
                    catch (Exception ex)
                    {
                        result["msg"] = $"�߽Ž��� : {ex.Message}";
                    }
                }
            }

            return result;
        }
    }
}
