//using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class lot_routing_sequence : BasePageModel
    {
        private readonly ILogger<lot_routing_sequence> _logger;

        public lot_routing_sequence(ILogger<lot_routing_sequence> logger)
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
            _logger = logger;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                Vali vali = new Vali(terms);
                vali.Null("ITEM_CODE", "ITEM_CODE�� �Էµ��� �ʾҽ��ϴ�.");

                vali.DoneDeco();

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                Params data = e.Params["data"];

                Vali vali = new Vali(data);
                vali.Null("WRK_CLS_CD", "�۾��з��ڵ尡 �Էµ��� �ʾҽ��ϴ�.");
                vali.Null("WRK_CLS_NM", "�۾��з����� �Էµ��� �ʾҽ��ϴ�.");
                //vali.Null("VIEW_YN", "���̱� ���ΰ� �Էµ��� �ʾҽ��ϴ�.");

                vali.DoneDeco();

                this.Save(data);


                // ������ ����
            }

            else if(e.Command == "delete")
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
        private DataTable Search(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
select  wc.item_code                                                      -- ITEM CODE
      , wc.job_id
      , wc.job_name                                                       -- Lot ID
      , pb.op_seq_num                                                     -- Oper SEQ
--      , bc.dept_class_code
--      , bc.dept_class_name                                                -- ���� �׷�   ���� Ȯ�� �� �� ����                                                    
      , wc.quantity                                       as qty          -- QTY PCS
      , case when wc.bu = 'SPS' then substr(pw.dept_name, 1, 3)
             else 'HDI'
        end                                               as site_id      -- SITE
      , pw.dept_code
      , pw.dept_name                                                      -- DEPT NAME
      , pb.resource_code                                  as mac_id
      , pb.description                                    as mac_name     -- MACHINE
      , to_char(po.work_date, 'yyyy-MM-dd')               as work_date    -- WORK DAY
      , to_char(pw.clock_accept, 'yyyy-MM-dd hh24:mi:ss') as accept_date  -- ACCEPT
      , to_char(pw.clock_in, 'yyyy-MM-dd hh24:mi:ss')     as strt_date    -- START
      , to_char(pw.clock_end, 'yyyy-MM-dd hh24:mi:ss')    as end_date     -- END
      , to_char(pw.clock_out, 'yyyy-MM-dd hh24:mi:ss')    as out_date     -- OUT
from  wip_closing_temp            wc
join  pwork_order                 po
on    wc.job_id           = po.job_id
join  product_bom_resource        pb
on    wc.job_id           = pb.job_id
and   pb.uom_code         != 'HR'
join  tb_bom_dept                 bd
on    wc.dept_code        = bd.dept_code
and   po.org_id           = bd.organization_id
--join  tb_bom_dept_class           bc                                    -- ���� Ȯ�� �Ŀ� �� ����
--on    bd.dept_class_code  = bc.dept_class_code
--and   bd.organization_id  = bc.organization_id
left outer join  product_working  pw
on    pb.job_id           = pw.job_id
and   pb.op_seq_num       = pw.operation_seq_num
where wc.yyyymmdd         = '20240601'   -- Cutoff ��¥ ���� ��ȸ (����Ʈ ����) ���� ��¥ ���� �޾ƿ;���
");
            /*
             * ������ ����
             */
            if (terms["group_id"].Length > 0 ) // �����(�׷�) ����
            {
                sSQL.Append($@"
AND GROUP_NAME = {terms["group_id"].V}
");
            }

            if (terms["ITEM_CODE"].Length > 0) // ITEM_CODE ����
            {
                sSQL.Append($@"
and   wc.item_code        = {terms["ITEM_CODE"].V} -- Item code�� �⺻ ��ȸ �������� �ʼ� �Է��ϵ��� ��. 
");
            }

            sSQL.Append(@"
order by  wc.item_code
        , wc.job_id
        , pb.op_seq_num
");

            string[] mergeCols = new[] { "ITEM_CODE", "JOB_ID" };

            DataTable result = Data.Get("MES", sSQL.ToString()).Tables[0];

            FormatForCellMerge(result, mergeCols);

            return result;
        }

        /// <summary>
        /// ��ȸ ���� 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private Params search_chart(Params terms)
        {
            Params result = new();

            return result;
        }


        /// <summary>
        /// ���� ���� 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(Params data)
        {
            //HS.Web.Proc.SAF_WRK_CLS.Save(data);
        }

        /// <summary>
        /// ������ �׸� ����
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void delete(ParamList data)
        {
            throw new Exception("�غ����Դϴ�.");

            StringBuilder sSQL = new StringBuilder();

            data.ForEach(D =>
            {
                sSQL.Append($@"
DELETE FROM SI_CODE_GROUP WHERE CMP_CD = {D["CMP_CD"].V} AND GRP_CD = {D["GRP_CD"].V};
");
            });

            HS.Web.Common.Data.Execute(sSQL.ToString());
        }



        /// <summary>
        /// �׸��� ����÷� �ɼ� ����
        /// </summary>
        /// <param name="dataList"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveGrid(ParamList dataList)
        {
            HS.Web.Proc.SI_GRID.Save(dataList);
        }


        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();

            if (command == "ExcelDownload")
            {
                //������ ��ȸ�� ������ ���� �ٿ�ε�
                DataTable dtResult = this.Search(e.Params["terms"]);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                return HS.Core.Excel.Download(dtResult, "Lot_Routing_Sequence_" + timestamp);
            }
            else
                return Page();
        }

        // CELL ����ó�� ���̱�
        public static DataTable FormatForCellMerge(DataTable dt, string[] mergeColumns)
        {
            if (dt.Rows.Count == 0) return dt;

            foreach (string colName in mergeColumns)
            {
                string prevValue = dt.Rows[0][colName]?.ToString();

                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    string currentValue = dt.Rows[i][colName]?.ToString();

                    if (currentValue == prevValue)
                    {
                        dt.Rows[i][colName] = DBNull.Value; // ���� ���̸� null ó��
                    }
                    else
                    {
                        prevValue = currentValue; // ���ο� ���̸� ������Ʈ
                    }
                }
            }

            return dt;
        }
    }
}
