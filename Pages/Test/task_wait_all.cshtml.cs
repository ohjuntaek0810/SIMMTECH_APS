using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace APS_SIMMTECH.Pages.Test
{
    public class task_wait_allModel : BasePageModel
    {
        public task_wait_allModel()
        {
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                // ����1
                var Task1 = Task.Run(() =>
                {
                    return Data.Get("SELECT * FROM ���̺�A").Tables[0].ToParamList();
                });

                // ����2
                var Task2 = Task.Run(() =>
                {
                    return Data.Get("SELECT * FROM ���̺�B").Tables[0].ToParamList();
                });

                // ����3
                var Task3 = Task.Run(() =>
                {
                    return Data.Get("SELECT * FROM ���̺�C").Tables[0].ToParamList();
                });

                // ��� ������ ���� �Ϸ� �Ǳ� ��ٸ� 
                Task.WaitAll(Task1, Task2, Task3); // �Ķ��Ÿ ���ڴ� ���� ����

                // Ÿ�� �ƿ� ����� 
                //if(Task.WaitAll(new Task[] { Task1, Task2, Task3 }, 10000) == false) // 10���� Ÿ�� �ƿ�
                //    throw new Exception("������ �ð��� �ʰ� �Ͽ����ϴ�.(10��)"); 


                toClient["result1"] = Task1.Result;
                toClient["result2"] = Task2.Result;
                toClient["result3"] = Task3.Result;
            }

            return toClient;
        }
    }
}
