using HS.Core;
using HS.Web.Common;
using System.Data;
using System.Text;

namespace HS.Web.Pages
{
    public class example_002 : BasePageModel
    {
        public example_002()
        {
            this.Handler = handler;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }


            else if (e.Command == "search_detail")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search_detail(terms);
            }

            else if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("WRK_CLS_CD", "작업분류코드가 입력되지 않았습니다.");
                vali.Null("WRK_CLS_NM", "작업분류명이 입력되지 않았습니다.");
                //vali.Null("VIEW_YN", "보이기 여부가 입력되지 않았습니다.");

                vali.DoneDeco();

                this.Save(data);


                // 데이터 저장
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
        private DataTable Search(Params terms)
        {

            return new ParamList(@"
[
    { colA: ""가"", colB: ""1001"", colC: ""a"", colD: 100, colE: ""2024-01-01"", colF: true, colG: ""2024-01-01 12:30:00"", colH: ""버튼테스트"" },
    { colA: ""나"", colB: ""2002"", colC: ""b"", colD: 99.56, colE: ""2024-01-01"", colF: 0, colG: ""2024-01-02 12:30:00"" },
    { colA: ""다"", colB: ""3003"", colC: ""c"", colD: -900.1234, colE: ""2024-01-01"", colF: null, colG: ""2024-01-02 12:30:00"" },
    { colA: ""라"", colB: ""4004"", colC: ""d"", colD: -900.1234, colE: ""2024-01-01"", colF: null, colG: ""2024-01-02 12:30:00"" }
]
").ToDataTable();
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable Search_detail(Params terms)
        {

            DataTable dt = new DataTable();

            dt.Columns.Add("colA", typeof(int));
            dt.Columns.Add("colB", typeof(string));
            dt.Columns.Add("colC", typeof(string));
            dt.Columns.Add("colD", typeof(string));
            dt.Columns.Add("colE", typeof(string));
            dt.Columns.Add("colF", typeof(string));
            dt.Columns.Add("colG", typeof(string));
            dt.Columns.Add("colH", typeof(string));

            for (int i = 1; i <= 100; i++)
            {
                DataRow dr = dt.NewRow();

                dr["colA"] = i;
                dr["colB"] = terms["colC"].AsString();
                dr["colC"] = terms["colC"].AsString();
                dr["colD"] = terms["colC"].AsString();
                dr["colE"] = terms["colC"].AsString();
                dr["colF"] = terms["colC"].AsString();
                dr["colG"] = terms["colC"].AsString();
                dr["colH"] = terms["colC"].AsString();

                dt.Rows.Add(dr);
            }




            return dt;
        }


        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            //HS.Web.Proc.SAF_WRK_CLS.Save(data);
        }

        /// <summary>
        /// 선택한 항목 삭제
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            throw new Exception("준비중입니다.");

            StringBuilder sSQL = new StringBuilder();

            data.ForEach(D =>
            {
                sSQL.Append($@"
DELETE FROM SI_CODE_GROUP WHERE CMP_CD = {D["CMP_CD"].V} AND GRP_CD = {D["GRP_CD"].V};
");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }
    }
}
