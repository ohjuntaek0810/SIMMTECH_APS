using DocumentFormat.OpenXml.Spreadsheet;
//using GrapeCity.DataVisualization.Chart;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HS.Web.Pages
{
    public class department_master : BasePageModel
    {

        public department_master()
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "view")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.Search(terms);
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];

                // ������ ����
                this.Save(dataList);
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
SELECT	--ORGANIZATION_ID, -- ����Ʈ ����
		A.DIVISION_ID AS ""GROUP"", 
        A.DEPARTMENT_CLASS_CODE as ""CLASS CODE"", 
        A.DEPARTMENT_CLASS_NAME as ""CLASS NAME"", --DEPARTMENT_ID, 
		A.DEPARTMENT_CODE as ""DEPARTMENT CODE"", 
        DEPARTMENT_NAME AS ""DEPARTMENT NAME"",
		--case when (DEPARTMENT_NAME like 'F%' or DEPARTMENT_NAME like 'S%') and (DEPARTMENT_NAME not like 'FS %') then SUBSTRING(DEPARTMENT_NAME, 1, 3) 
		--     when (DEPARTMENT_CLASS_NAME like 'F%' or DEPARTMENT_CLASS_NAME like 'S%') and (DEPARTMENT_CLASS_NAME not like 'FS %') then SUBSTRING(DEPARTMENT_CLASS_NAME, 1, 3) 
		--	 else '-' 
		--end as SITE,  --> I/F �� ���� �������� �� ��.
		A.SITE_ID AS SITE,  -- �÷� �߰� 		
		--case when (DEPARTMENT_NAME like 'F%' or DEPARTMENT_NAME like 'S%') and (DEPARTMENT_NAME not like 'FS %') and (DEPARTMENT_NAME not like 'F53.%') then SUBSTRING(DEPARTMENT_NAME, 5, len(DEPARTMENT_NAME) -4) 
		--	 when (DEPARTMENT_NAME like 'F%' or DEPARTMENT_NAME like 'S%') and (DEPARTMENT_NAME not like 'FS %') and (DEPARTMENT_NAME like 'F53.%') then SUBSTRING(DEPARTMENT_NAME, 6, len(DEPARTMENT_NAME) -5) 
		--	 else DEPARTMENT_NAME
		--end as ROUTE_NAME,  --> I/F �� ���� �������� �� ��.
		A.ROUTE_NAME AS ""ROUTING NAME"",
		----CREATION_DTTM, DISABLE_DATE, COST_ENABLED, ROUTE_NAME, 
		A.APS_WIP_ROUTE_GRP_ID, B.WIP_ROUTE_GROUP_NAME AS ""WIP STATUS GROUP"",			-- ID �÷��� ����Ʈ ���� 
		A.APS_DEPT_GRP_ID, C.APS_DEPT_GRP_NAME AS ""DEPARTMENT GROUP"",					-- ID �÷��� ����Ʈ ���� 
		A.RESOURCE_CAPA_GROUP_ID, D.RESOURCE_CAPA_GROUP_NAME AS ""RESOURCE CAPA NAME"",   -- ID �÷��� ����Ʈ ���� 
		--PART_LEADER, CLASS_GROUP, 		
		D.OUTSOURCE_YN AS ""OUTSOURCE(Y/N)"", 
        A.OWN_OUT_SELECT_YN, 
		A.USE_YN AS ""USE(Y/N)"", -- case when disable_date is not null then 'N' else 'Y' end as USE_YN  ���� �ʱ�ȭ��. ���� I/F �ÿ���  'R'�� �ʱ�ȭ. 
		A.INSERT_DTTM, A.INSERT_ID,		-- ����Ʈ ����
		A.UPDATE_DTTM, A.UPDATE_ID
  FROM [APS].[dbo].[TH_TAR_DEPT_MASTER] A
		left outer join 
		(
			SELECT SEGMENT1 as WIP_ROUTE_GROUP_ID, ATTRIBUTE01 as WIP_ROUTE_GROUP_NAME, sort_order
			FROM [APS].[dbo].[LOOKUP_VALUE_M]
			where lookup_type_code = 'WIP_ROUTE_GROUP'
			and LOOKUP_TYPE_VERSION = (
										SELECT LOOKUP_TYPE_VERSION
										FROM [APS].[dbo].[LOOKUP_TYPE_M]
										where lookup_type_code = 'WIP_ROUTE_GROUP'
										and ACTIVE_FLAG = 'Y'
									  )
			and ACTIVE_FLAG = 'Y'
		) B
		on A.APS_WIP_ROUTE_GRP_ID = B.WIP_ROUTE_GROUP_ID
		left outer join 
		( -- APS_WIP_ROUTE_GRP_ID - Name 
			SELECT SEGMENT1 as APS_DEPT_GRP_ID, ATTRIBUTE01 as APS_DEPT_GRP_NAME, sort_order
			FROM [APS].[dbo].[LOOKUP_VALUE_M]
			where lookup_type_code = 'APS_DEPT_GRP'
			and LOOKUP_TYPE_VERSION = (
										SELECT LOOKUP_TYPE_VERSION
										FROM [APS].[dbo].[LOOKUP_TYPE_M]
										where lookup_type_code = 'APS_DEPT_GRP'
										and ACTIVE_FLAG = 'Y'
									  )
			and ACTIVE_FLAG = 'Y'
		) C
		on A.APS_DEPT_GRP_ID = C.APS_DEPT_GRP_ID
		left outer join 
		( -- RESOURCE_CAPA_GROUP_ID - Name 
			SELECT SEGMENT1 as RESOURCE_CAPA_GROUP_ID, ATTRIBUTE01 as RESOURCE_CAPA_GROUP_NAME, sort_order, ATTRIBUTE05 AS OUTSOURCE_YN
			FROM [APS].[dbo].[LOOKUP_VALUE_M]
			where lookup_type_code = 'RESOURCE_CAPA_GROUP'
			and LOOKUP_TYPE_VERSION = (
										SELECT LOOKUP_TYPE_VERSION
										FROM [APS].[dbo].[LOOKUP_TYPE_M]
										where lookup_type_code = 'RESOURCE_CAPA_GROUP'
										and ACTIVE_FLAG = 'Y'
									  )
			and ACTIVE_FLAG = 'Y'
		) D
		on A.RESOURCE_CAPA_GROUP_ID = D.RESOURCE_CAPA_GROUP_ID
	where ORGANIZATION_ID = 101 
");
            if (terms["group_id"].Length > 0)
            {
                sSQL.Append($@"
    AND DIVISION_ID = {terms["group_id"].V}
");
            }

            if (terms["dept_class_name"].Length > 0)
            {
                sSQL.Append($@"
    AND A.DEPARTMENT_CLASS_CODE = {terms["dept_class_name"].V}
");
            }

            if (terms["dept_name"].Length > 0)
            {
                sSQL.Append($@"
    AND A.DEPARTMENT_CODE = {terms["dept_name"].V}
");
            }

            if (terms["aps_wip_route_group_name"].Length > 0)
            {
                sSQL.Append($@"
    AND A.APS_WIP_ROUTE_GRP_ID = {terms["aps_wip_route_group_name"].V}
");
            }

            if (terms["aps_dept_group_name"].Length > 0)
            {
                sSQL.Append($@"
    AND A.APS_DEPT_GRP_ID = {terms["aps_dept_group_name"].V}
");
            }

            if (terms["resource_capa_group_name"].Length > 0)
            {
                sSQL.Append($@"
    AND A.RESOURCE_CAPA_GROUP_ID = {terms["resource_capa_group_name"].V}
");
            }

            if (terms["input_yn"].AsBool())
            {
                sSQL.Append($@"
    AND (
        A.APS_WIP_ROUTE_GRP_ID IS NULL OR
        A.APS_DEPT_GRP_ID IS NULL OR
        A.RESOURCE_CAPA_GROUP_ID IS NULL
    )
");
            }

            sSQL.Append($@"
    order by case when use_yn != 'Y' and use_yn != 'N' then 1 else 2 end, --  �ű� �׸� I/F �� ���� ��� �ֻ�ܿ� ǥ��  (USE_YN = 'R', Registration required) 
		     department_code
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
            HS.Web.Proc.TH_TAR_DEPT_MASTER.Save(data);
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

                return HS.Core.Excel.Download(dtResult, "Resource_Master_" + timestamp);
            }
            else
                return Page();
        }
    }
}
