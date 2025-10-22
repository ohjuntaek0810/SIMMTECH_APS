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
                // 쿼리1
                var Task1 = Task.Run(() =>
                {
                    return Data.Get("SELECT * FROM 테이블A").Tables[0].ToParamList();
                });

                // 쿼리2
                var Task2 = Task.Run(() =>
                {
                    return Data.Get("SELECT * FROM 테이블B").Tables[0].ToParamList();
                });

                // 쿼리3
                var Task3 = Task.Run(() =>
                {
                    return Data.Get("SELECT * FROM 테이블C").Tables[0].ToParamList();
                });

                // 모든 쿼리가 실행 완료 되길 기다림 
                Task.WaitAll(Task1, Task2, Task3); // 파라메타 숫자는 관계 없음

                // 타임 아웃 적용시 
                //if(Task.WaitAll(new Task[] { Task1, Task2, Task3 }, 10000) == false) // 10초의 타임 아웃
                //    throw new Exception("지정된 시간이 초과 하였습니다.(10초)"); 


                toClient["result1"] = Task1.Result;
                toClient["result2"] = Task2.Result;
                toClient["result3"] = Task3.Result;
            }

            return toClient;
        }
    }
}
