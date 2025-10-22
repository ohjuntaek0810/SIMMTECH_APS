using DocumentFormat.OpenXml.Drawing;
using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Net;
using System.Text;

namespace HS.Web.Pages
{
    public class item_master : BasePageModel
    {
        private readonly ILogger<item_master> _logger;


        public item_master(ILogger<item_master> logger)
        {
            this.Handler = handler;
            this.OnPostHandler = OnPostPage;
            _logger = logger;
        }

        private Params handler(PostAjaxArgs e)
        {
            Params toClient = new Params();

            if (e.Command == "search_header_basic")
            {
                Params data = e.Params["terms"];

                toClient["data"] = this.SearchBasicHeaderColumn(data);
            }

            if (e.Command == "search_header_column")
            {
                toClient["data"] = this.SearchHeaderColumn();
            }

            if (e.Command == "search_grid")
            {
                Params data = e.Params["terms"];

                toClient["data"] = this.SearchGrid(data);
            }

            if (e.Command == "search")
            {
                Params terms = e.Params["terms"];
                ParamList headers = e.Params["headers"];

                toClient["data"] = this.SearchBasicInfo(terms, headers);
            }


            else if (e.Command == "search_chart")
            {
                Params terms = e.Params["terms"];

                toClient = this.search_chart(terms);
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

            if (e.Command == "check_favorite")
            {
                Params data = e.Params["terms"];

                toClient["data"] = CmnClient.checkFavorite(data);

            }

            if (e.Command == "add_favorite")
            {
                Params data = e.Params["terms"];

                CmnClient.addFavorite(data);
            }

            if (e.Command == "delete_favorite")
            {
                Params data = e.Params["terms"];

                CmnClient.deleteFavorite(data);
            }

            if (e.Command == "search_grid")
            {
                Params data = e.Params["terms"];

                toClient["data"] = CmnClient.SearchGrid(data);
            }

            if (e.Command == "save_grid")
            {
                ParamList dataList = e.Params["data"];

                CmnClient.SaveGrid(dataList);
            }

            return toClient;
        }

        /// <summary>
        /// 조회 로직 GROUP 1,2,6
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private DataTable SearchBasicInfo(Params terms, ParamList headers)
        {

            DTClient.UserInfoMerge(terms);

            // 쿼리1
            var Task1 = Task.Run(() =>
            {
                var sSQL = new StringBuilder();
                sSQL.Append($@"
-- Group 1, 2 
SELECT  
    case when substr(csb.d_category, 1, 1) = 'S' then 'SPS' else 'HDI' end as ""Division""
      , csb.ITEM_CODE AS ""Item no""
      , csb.REVISION AS ""Rev""
      --, csb.D_CATEGORY 
      , rtrim(substr(MCV.description, instr(MCV.description, '.', 1, 2) + 1, length(MCV.description))) as ""Category""
      , csb.D_LAYER as ""Layer""
      , cdi.MFG_CATEGORY AS ""MFG Category""
      , '' ORDER_TYPE 
      , csb.CUSTOMER AS ""Customer Num""
      --, AC.CUSTOMER_NAME AS ""Customer""
      , csb.MODEL_NAME AS ""Model name""
      --, csc2.customized_code  -- 
      , cclH.DESCRIPTION as ""Customaized""
      , CITC.PROJECT_NAME as Project 
      , csb.THICK_T       as ""Thick""
      --, csb.FINISH_CODE
      , CBL.DESCRIPTION   AS ""Finish""
      , csb.Q_BLOCK_X   as ""Pcs/Strip X""        -- 21
      , csb.Q_BLOCK_Y   as ""Pcs/Strip Y""        -- 22 
      , csb.Q_ARRAY_U   as ""Up""                   -- 23 
      , csb.Q_ARRAY_X   as ""Strip/Pnl X""        -- 24 
      , csb.Q_ARRAY_Y   as ""Strip/Pnl Y""        -- 25
      , csb.Q_BLOCK_U   as ""Blk Qty""            -- 26
      , csb.UNIT_X as ""Unit Size X"" -- 27 
      , csb.UNIT_Y as ""Unit Size Y"" -- 28 
      , csb.ARRAY_X as ""Strip Size X""    -- 29
      , csb.ARRAY_Y as ""Strip Size Y""    -- 30
      -- , csb.W_PANEL_CODE
      , (CCL.ATTRIBUTE1 * CCLB.ATTRIBUTE2) / 1000000 AS ""Pnl/SQM""
      , CCL.ATTRIBUTE1      AS ""Panel Size X""
      , CCLB.ATTRIBUTE2     AS ""Panel Size Y"" 
      , csb.Q_ARRAY_U * csb.Q_ARRAY_X	* csb.Q_ARRAY_Y as ""Pcs/Pnl""  -- 33 
      , csb.SPEC_UNIT_NO  AS ""Unit No"" --  as UNIT_NO      -- 34 
      , csb.SPEC_ARRAY_NO  as ""Strip No""     -- 35 
      --, csc2.MAT_TYPE_CODE
      , cclC.description as ""Material Type"" -- 36
      --, csc2.lay_up_type_code
      , cclD.description as ""Lay-up Type""   -- 37 
      --, csc2.ddr_type_code 
      , cclE.description as DDR           -- 38
      , csc2.BALL_PAD_COUNT AS ""Ball Pad Count""               -- 39
      , csc2.GRADE as ""Grade""               -- 40
      , csc2.X_OUT_STRIP AS ""X-out Strip""                  -- 41
      , MSIB.description  as ""S/M Material"" -- as ""S/M Material""   -- 45
      , cclF.description  as ""S/M Color""   -- ""S/M Color"" -- 46 
      , cclG.description  as ITS_LOCATION  -- 75
      , SPS.DESCRIPTION   AS ""SPS Grade""     -- 77
FROM   CBST_SPEC_BASIC csb
       INNER JOIN 
       (
       SELECT ORGANIZATION_ID
            , ITEM_CODE
            , MAX(REVISION) AS REVISION
      FROM  CBST_SPEC_BASIC
      WHERE ORGANIZATION_ID = 101
      GROUP BY  ORGANIZATION_ID
              , ITEM_CODE
      ) B
ON    CSB.ORGANIZATION_ID = B.ORGANIZATION_ID
AND   CSB.ITEM_CODE       = B.ITEM_CODE
AND   CSB.REVISION        = B.REVISION
      LEFT OUTER JOIN
      MTL_CATEGORIES_V MCV
ON    csb.D_CATEGORY   = MCV.CATEGORY_CONCAT_SEGS
AND   MCV.STRUCTURE_NAME = 'Item Categories'
--      LEFT OUTER JOIN
--      ar_customers AC
--ON    csb.CUSTOMER = AC.customer_number
      LEFT OUTER JOIN
      CBOM_ITEM_CUSTOMER_INFO CITC 
ON    csb.item_code = CITC.item_code
      LEFT OUTER JOIN
      CBST_COMMON_L  CBL
ON    CBL.line_code   = csb.FINISH_CODE
AND   CBL.LOOKUP_TYPE = 'FINISH'
      LEFT OUTER JOIN
      CBST_COMMON_L  CCL 
ON    csb.W_PANEL_CODE  = CCL.LINE_CODE
AND   CCL.LOOKUP_TYPE = 'W Panel Size'
      LEFT OUTER JOIN
      CBST_COMMON_L  CCLB 
ON    csb.W_PANEL_CODE  = CCLB.LINE_CODE   
AND   CCLB.LOOKUP_TYPE = 'W Panel Size' 
      LEFT OUTER JOIN 
      CBST_DRC_INFO cdi
on    csb.organization_id = cdi.organization_id
and   csb.item_code = cdi.item_code
      LEFT OUTER JOIN 
      cbst_spec_capa2 csc2 
on    csb.organization_id =  csc2.organization_id
and   csb.item_code = csc2.item_code
and   csb.revision  = csc2.revision
      LEFT OUTER JOIN
      cbst_common_l cclH 
ON    csc2.customized_code = cclH.line_code     
AND   cclH.lookup_type = 'Customized' 
AND   cclH.enabled_flag = 'Y' 
      LEFT OUTER JOIN
      cbst_common_l cclC 
ON    csc2.MAT_TYPE_CODE = cclC.line_code     
AND   cclC.lookup_type = 'Material type' 
AND   cclC.enabled_flag = 'Y' 
      LEFT OUTER JOIN
      cbst_common_l cclD
ON    case when substr(csb.d_category, 1, 1) = 'S' then 'SPS' else 'HDI' end = cclD.attribute1
AND   csc2.lay_up_type_code = cclD.line_code  
AND   cclD.lookup_type      = 'Lay-up'
AND   cclD.enabled_flag = 'Y'
      LEFT OUTER JOIN
      cbst_common_l cclE
ON    csc2.ddr_type_code = cclE.line_code
AND   cclE.lookup_type = 'DDR Type'
AND   cclE.enabled_flag = 'Y'
      LEFT OUTER JOIN
      mtl_system_items_b MSIB
ON    csb.organization_id = MSIB.organization_id   
AND   csc2.smask_material = MSIB.inventory_item_id    
      LEFT OUTER JOIN
      Cbst_common_l cclF
ON    csc2.SMASK_COLOR  =   cclF.line_code   
AND   cclF.lookup_type = 'S/M Color'
AND   cclF.enabled_flag = 'Y'
      LEFT OUTER JOIN
      cbst_common_l cclG
ON    csb.ATTRIBUTE1  = cclG.line_code    
AND   cclF.lookup_type = 'ITS_LOCATION'
AND   cclF.enabled_flag = 'Y'
      LEFT OUTER JOIN
      ( 
      SELECT  S1.ORGANIZATION_ID
            , S1.ITEM_CODE
            , S1.REVISION 
            , MAX(C1.DESCRIPTION) AS DESCRIPTION
      FROM   CBST_SPEC_CAPA1 S1,
             CBST_COMMON_L C1
      WHERE  S1.LOV_CODE = C1.LINE_CODE
      AND    S1.CAPA_GROUP_CODE = 160 
      AND    C1.LOOKUP_TYPE = 'CAPA_SPS Grade'
      GROUP BY  S1.ORGANIZATION_ID
              , S1.ITEM_CODE
              , S1.REVISION 
      )  SPS
ON    CSB.ORGANIZATION_ID = SPS.ORGANIZATION_ID
AND   CSB.ITEM_CODE       = SPS.ITEM_CODE
AND   CSB.REVISION        = SPS.REVISION
WHERE 1 = 1
and   csb.organization_id = 101
--and substr(csb.d_category, 1, 1) = 'S' -- DIVISION = 'SPS' 인 경우의 필터 조건 추가,  DIVISION = 'HDI' 이면 'H'
--and (csb.item_code = 'MCP20609H00' and csb.revision = '001')
");

                /*
                 * 조건절 시작
                 */
                if (terms["division"].AsString() == "SPS") // division 조건
                {
                    sSQL.Append($@"
and substr(csb.d_category, 1, 1) = 'S' -- DIVISION = 'SPS' 인 경우의 필터 조건 추가,  DIVISION = 'HDI' 이면 'H'
");
                } else if (terms["division"].AsString() == "HDI")   // division 조건
                {
                    sSQL.Append($@"
and substr(csb.d_category, 1, 1) = 'H' -- DIVISION = 'SPS' 인 경우의 필터 조건 추가,  DIVISION = 'HDI' 이면 'H'
");
                } 

                    if (terms["item_no"].Length > 0) // item_no 조건
                {
                    sSQL.Append($@"
AND csb.ITEM_CODE LIKE  '%{terms["item_no"].AsString()}%'
");
                }
                sSQL.Append($@"
ORDER BY ""Division"" desc, ""Item no"", ""Rev"" desc
");

                return Data.Get("ERP", sSQL.ToString()).Tables[0];
            });


            // 이 headers로 LOOKUP_VALUE_M 테이블을 만들어서 아래 쿼리를 돌려야한다.
            StringBuilder str_lookup_value = new StringBuilder();

            int index = 0;
            int lastIndex = headers.Count - 1;
            headers.ForEach(ITEM =>
            {
                str_lookup_value.Append($@"
select 
{ITEM["LOOKUP_TYPE_CODE"].V} AS LOOKUP_TYPE_CODE,
{ITEM["LOOKUP_TYPE_VERSION"].V} AS LOOKUP_TYPE_VERSION,
{ITEM["IS_LATEST_VERSION_YN"].V} AS IS_LATEST_VERSION_YN,
{ITEM["SEGMENT1"].V} AS SEGMENT1,
{ITEM["SEGMENT2"].V} AS SEGMENT2,
{ITEM["SEGMENT3"].V} AS SEGMENT3,
{ITEM["SEGMENT4"].V} AS SEGMENT4,
{ITEM["SEGMENT5"].V} AS SEGMENT5,
{ITEM["SEGMENT6"].V} AS SEGMENT6,
{ITEM["SEGMENT7"].V} AS SEGMENT7,
{ITEM["SEGMENT8"].V} AS SEGMENT8,
{ITEM["ATTRIBUTE01"].V} AS ATTRIBUTE01,
{ITEM["ATTRIBUTE02"].V} AS ATTRIBUTE02,
{ITEM["ATTRIBUTE03"].V} AS ATTRIBUTE03,
{ITEM["ATTRIBUTE04"].V} AS ATTRIBUTE04,
{ITEM["ATTRIBUTE05"].V} AS ATTRIBUTE05,
{ITEM["ATTRIBUTE06"].V} AS ATTRIBUTE06,
{ITEM["ATTRIBUTE07"].V} AS ATTRIBUTE07,
{ITEM["ATTRIBUTE08"].V} AS ATTRIBUTE08,
{ITEM["ATTRIBUTE09"].V} AS ATTRIBUTE09,
{ITEM["ATTRIBUTE10"].V} AS ATTRIBUTE10,
{ITEM["ATTRIBUTE11"].V} AS ATTRIBUTE11,
{ITEM["ATTRIBUTE12"].V} AS ATTRIBUTE12,
{ITEM["ATTRIBUTE13"].V} AS ATTRIBUTE13,
{ITEM["ATTRIBUTE14"].V} AS ATTRIBUTE14,
{ITEM["ATTRIBUTE15"].V} AS ATTRIBUTE15,
{ITEM["ACTIVE_FLAG"].V} AS ACTIVE_FLAG,
{ITEM["SORT_ORDER"].V} AS SORT_ORDER,
{ITEM["VALUE_DESCRIPTION"].V} AS VALUE_DESCRIPTION from dual
");

                // last index가 아니면 union all 추가필요
                if(index != lastIndex)
                {
                    str_lookup_value.Append(@"
union all
");
                }
                index++;
            });

            // 쿼리2 : Special Process 그룹
            // TODO : LINE 코드가 내부 LOOKUP_VALUE에 없는데... 처리방법 문의필요
            // TODO : 조회조건 추가 필요
            var Task2 = Task.Run(() =>
            {
                var sSQL = new StringBuilder();
                sSQL.Append($@"
with item_mst_lookup_VALUE_M as (
    {str_lookup_value.ToString()}
), 
ITEM_MASTER_UI_COL_LOOKUP_LIST as (
    select  LOOKUP_TYPE_CODE, 
            SEGMENT1 as COLUMN_CANDIDATE_ID,
            ATTRIBUTE01 as UI_COLUMN_ORDER, 
            ATTRIBUTE02 as ITEM_MASTER_COLUMN_NAME,
            ATTRIBUTE03 as Type_Group,
            ATTRIBUTE04 as CAPA_GROUP_LV1_CODE,
            ATTRIBUTE05 as CAPA_GROUP_LV2_CODE,
            ATTRIBUTE06 as CAPA_GROUP_LV1_NAME,
            ATTRIBUTE07 as CAPA_GROUP_LV2_NAME,
            ATTRIBUTE08 as CAPA_GROUP_LV1_TYPE,
            ATTRIBUTE09 as LINE_ID,
            ATTRIBUTE10 as UI_VIEW_LEVEL,
            ATTRIBUTE11 as SOURCE,
            SORT_ORDER, 
            ACTIVE_FLAG
    from item_mst_lookup_VALUE_M
    where LOOKUP_TYPE_CODE = 'ITEM_MASTER_UI_COLUMN_CANDIDATE'
    and nvl(ACTIVE_FLAG, 'N') = 'Y' 
) --select * from ITEM_MASTER_UI_COL_LOOKUP_LIST order by UI_COLUMN_ORDER; 
,
SPECIAL_PROCESS_FOR_UI_LOOKUP as (
    select --* 
             ITEM_MASTER_COLUMN_NAME, CAPA_GROUP_LV1_CODE as line_code, LINE_ID, sort_order, UI_COLUMN_ORDER -- , CAPA_GROUP_LV1_NAME   --> ERP 룩업의 description이 CAPA_GROUP_LV1_NAME, ITEM_MASTER_COLUMN_NAME 은 APS용으로 컬럼 이름 정한 것. 
    from ITEM_MASTER_UI_COL_LOOKUP_LIST 
    where Type_Group = 'Group7_Special_Process'
    --and  NVL(ACTIVE_FLAG, 'N') = 'Y' 
    --order by UI_COLUMN_ORDER --to_number(CAPA_GROUP_LV1_CODE)
) -- select * from SPECIAL_PROCESS_FOR_UI_LOOKUP order by UI_COLUMN_ORDER;
,
SPECIAL_PROCESS_FOR_UI_LIST as (
    select * from SPECIAL_PROCESS_FOR_UI_LOOKUP 
    where line_code in (    select line_code FROM   cbst_common_l ccl  -- ERP 룩업에 있는 special process line_code만
                            WHERE  ccl.enabled_flag = 'Y'
                            and lookup_type = 'CAPA_Special process'  
                       )
    --order by UI_COLUMN_ORDER --to_number(CAPA_GROUP_LV1_CODE)
) --select * from  SPECIAL_PROCESS_FOR_UI_LIST order by UI_COLUMN_ORDER;  --> ITEM_MASTER_COLUMN_NAME 항목이 피벗 컬럼명 #####  ERP Lookup description 
,
ITEM_SPECIAL_PROCESS_LIST as (
    select ORGANIZATION_ID, ITEM_CODE, REVISION, CAPA_GROUP_ID, CAPA_GROUP_CODE, LOV_CODE
    from cbst_spec_capa1 cap1
    where 1=1
");
                if (terms["item_no"].Length > 0) // item_no 조건
                {
                    sSQL.Append($@"
    AND cap1.item_code  LIKE  '%{terms["item_no"].AsString()}%'
");
                }

                sSQL.Append($@"
    --AND    cap1.item_code = 'MCP20609H00' 
    --AND    cap1.revision = '000'
    AND    capa_group_code = (
                                SELECT LINE_CODE -- 90 Special process 
                                FROM   cbst_common_l ccl
                                WHERE  ccl.lookup_type = 'CAPA_GROUP'
                                AND    ccl.enabled_flag = 'Y'
                                and    ccl.description = 'Special process' 
                                and line_code in (  
                                                    select line_code from SPECIAL_PROCESS_FOR_UI_LIST
                                                 )
                             )
)
--select * from ITEM_SPECIAL_PROCESS_LIST;
,
item_line as (
    select ITEM_CODE, REVISION from ITEM_SPECIAL_PROCESS_LIST group by ITEM_CODE, REVISION
) --select * from item_line;
, 
crossjoin as 
(
    select A.*, b.*    
    from item_line A
         Cross join 
         SPECIAL_PROCESS_FOR_UI_LIST B
),
exists_list as (
    select A.ITEM_CODE, A.REVISION, A.ITEM_MASTER_COLUMN_NAME, A.LINE_CODE, A.LINE_ID, A.SORT_ORDER, A.UI_COLUMN_ORDER,
            C.lov_code, 
           --isnull2(C.lov_code, 'N', 'Y') as AAA 
           case when nvl(c.lov_code, 'N') ='N'  then 0 else 1 end as special_process_exists
    from crossjoin A
         left outer join 
         ITEM_SPECIAL_PROCESS_LIST C
    on a.item_code = c.item_code 
    and a.revision = c.revision 
    and a.line_code = c.LOV_CODE 
) --select * from exists_list ORDER BY UI_COLUMN_ORDER;
,
pv_list as (
    select --A.* --, 
            A.ITEM_CODE, A.REVISION, A.ITEM_MASTER_COLUMN_NAME, A.special_process_exists, a.sort_order 
    from exists_list A
    --order by sort_order
)
--SELECT * FROM pv_list;
,
aaa as (
    select * 
    from ( select  
                A.ITEM_CODE, A.REVISION, A.ITEM_MASTER_COLUMN_NAME, A.special_process_exists 
            from exists_list A
         )
    pivot (
            sum(special_process_exists) 
            for ITEM_MASTER_COLUMN_NAME in (  -- APS 룩업 컬럼명으로 표시하고 카운팅
                                'Tenting' AS ""Tenting"",
                                'Tenting(Inner)' AS ""Tenting(Inner)"",
                                'Tenting(Outer)' AS ""Tenting(Outer)"",
                                'PNL fill(Inner)' AS ""PNL fill(Inner)"",
                                'PNL fill(Outer)' AS ""PNL fill(Outer)"",
                                'MSAP Non fill(Inner)' AS ""MSAP Non fill(Inner)"",
                                'MSAP Non fill(Outer)' AS ""MSAP Non fill(Outer)"",
                                'MSAP fill(Inner)' AS ""MSAP fill(Inner)"",
                                'MSAP fill(Outer)' AS ""MSAP fill(Outer)"",
                                'SAP fill(Inner)' AS ""SAP fill(Inner)"",
                                'SAP fill(Outer)' AS ""SAP fill(Outer)"",
                                'Automotive' AS ""Automotive"",
                                'Etch Back' AS ""Etch Back"",
                                'SAP' AS ""SAP"",
                                'ETS' AS ""ETS"",
                                'Tailless' AS ""Tailless"",
                                'New Tailless' AS ""New Tailless"",
                                'SOP' AS ""SOP"",
                                'SEQUENTIAL' AS ""SEQUENTIAL"",
                                'Via fill' AS ""Via fill"",
                                'Skiving' AS ""Skiving"",
                                'Full Stack' AS ""Full Stack"",
                                'Coreless' AS ""Coreless"",
                                'VIP' AS ""VIP"",
                                'ITS' AS ""ITS"",
                                'Singulation Limit' AS ""Singulation Limit"",
                                'Impedance' AS ""Impedance"",
                                'SR Patch(On Cu)' AS ""SR Patch(On Cu)"",
                                'SR Patch(On SR)' AS ""SR Patch(On SR)"",
                                'SR Dam' AS ""SR Dam"",
                                'DRAM F/C' AS ""DRAM F/C"",
                                'MLCC' AS ""MLCC"",
                                'eMMC' AS ""eMMC"",
                                'eMCP' AS ""eMCP"",
                                '열보정' AS ""열보정"",
                                'F/C CSP' AS ""F/C CSP"",
                                'PoP' AS ""PoP"",
                                'SMS' AS ""SMS"",
                                'Flip Chip' AS ""Flip Chip"",
                                'EMBEDDED' AS ""EMBEDDED"",
                                'SIMS' AS ""SIMS"",
                                'VOP' AS ""VOP"",
                                'Slot punch' AS ""Slot punch"",
                                'B2IT' AS ""B2IT"",
                                '3 Roll Printing' AS ""3 Roll Printing"",
                                'Cap Plating' AS ""Cap Plating"",
                                'Perimeter' AS ""Perimeter"",
                                'SB' AS ""SB""
            )
        )) 
SELECT
    ITEM_CODE AS ""Item no"",
    REVISION AS Rev,
    CASE WHEN ""Tenting"" = 1 THEN 'Y' ELSE 'N' END AS ""Tenting"",
    CASE WHEN ""Tenting(Inner)"" = 1 THEN 'Y' ELSE 'N' END AS ""Tenting(Inner)"",
    CASE WHEN ""Tenting(Outer)"" = 1 THEN 'Y' ELSE 'N' END AS ""Tenting(Outer)"",
    CASE WHEN ""PNL fill(Inner)"" = 1 THEN 'Y' ELSE 'N' END AS ""PNL fill(Inner)"",
    CASE WHEN ""PNL fill(Outer)"" = 1 THEN 'Y' ELSE 'N' END AS ""PNL fill(Outer)"",
    CASE WHEN ""MSAP Non fill(Inner)"" = 1 THEN 'Y' ELSE 'N' END AS ""MSAP Non fill(Inner)"",
    CASE WHEN ""MSAP Non fill(Outer)"" = 1 THEN 'Y' ELSE 'N' END AS ""MSAP Non fill(Outer)"",
    CASE WHEN ""MSAP fill(Inner)"" = 1 THEN 'Y' ELSE 'N' END AS ""MSAP fill(Inner)"",
    CASE WHEN ""MSAP fill(Outer)"" = 1 THEN 'Y' ELSE 'N' END AS ""MSAP fill(Outer)"",
    CASE WHEN ""SAP fill(Inner)"" = 1 THEN 'Y' ELSE 'N' END AS ""SAP fill(Inner)"",
    CASE WHEN ""SAP fill(Outer)"" = 1 THEN 'Y' ELSE 'N' END AS ""SAP fill(Outer)"",
    CASE WHEN ""Automotive"" = 1 THEN 'Y' ELSE 'N' END AS ""Automotive"",
    CASE WHEN ""Etch Back"" = 1 THEN 'Y' ELSE 'N' END AS ""Etch Back"",
    CASE WHEN ""SAP"" = 1 THEN 'Y' ELSE 'N' END AS ""SAP"",
    CASE WHEN ""ETS"" = 1 THEN 'Y' ELSE 'N' END AS ""ETS"",
    CASE WHEN ""Tailless"" = 1 THEN 'Y' ELSE 'N' END AS ""Tailless"",
    CASE WHEN ""New Tailless"" = 1 THEN 'Y' ELSE 'N' END AS ""New Tailless"",
    CASE WHEN ""SOP"" = 1 THEN 'Y' ELSE 'N' END AS ""SOP"",
    CASE WHEN ""SEQUENTIAL"" = 1 THEN 'Y' ELSE 'N' END AS ""SEQUENTIAL"",
    CASE WHEN ""Via fill"" = 1 THEN 'Y' ELSE 'N' END AS ""Via fill"",
    CASE WHEN ""Skiving"" = 1 THEN 'Y' ELSE 'N' END AS ""Skiving"",
    CASE WHEN ""Full Stack"" = 1 THEN 'Y' ELSE 'N' END AS ""Full Stack"",
    CASE WHEN ""Coreless"" = 1 THEN 'Y' ELSE 'N' END AS ""Coreless"",
    CASE WHEN ""VIP"" = 1 THEN 'Y' ELSE 'N' END AS ""VIP"",
    CASE WHEN ""ITS"" = 1 THEN 'Y' ELSE 'N' END AS ""ITS"",
    CASE WHEN ""Singulation Limit"" = 1 THEN 'Y' ELSE 'N' END AS ""Singulation Limit"",
    CASE WHEN ""Impedance"" = 1 THEN 'Y' ELSE 'N' END AS ""Impedance"",
    CASE WHEN ""SR Patch(On Cu)"" = 1 THEN 'Y' ELSE 'N' END AS ""SR Patch(On Cu)"",
    CASE WHEN ""SR Patch(On SR)"" = 1 THEN 'Y' ELSE 'N' END AS ""SR Patch(On SR)"",
    CASE WHEN ""SR Dam"" = 1 THEN 'Y' ELSE 'N' END AS ""SR Dam"",
    CASE WHEN ""DRAM F/C"" = 1 THEN 'Y' ELSE 'N' END AS ""DRAM F/C"",
    CASE WHEN ""MLCC"" = 1 THEN 'Y' ELSE 'N' END AS ""MLCC"",
    CASE WHEN ""eMMC"" = 1 THEN 'Y' ELSE 'N' END AS ""eMMC"",
    CASE WHEN ""eMCP"" = 1 THEN 'Y' ELSE 'N' END AS ""eMCP"",
    CASE WHEN ""열보정"" = 1 THEN 'Y' ELSE 'N' END AS ""열보정"",
    CASE WHEN ""F/C CSP"" = 1 THEN 'Y' ELSE 'N' END AS ""F/C CSP"",
    CASE WHEN ""PoP"" = 1 THEN 'Y' ELSE 'N' END AS ""PoP"",
    CASE WHEN ""SMS"" = 1 THEN 'Y' ELSE 'N' END AS ""SMS"",
    CASE WHEN ""Flip Chip"" = 1 THEN 'Y' ELSE 'N' END AS ""Flip Chip"",
    CASE WHEN ""EMBEDDED"" = 1 THEN 'Y' ELSE 'N' END AS ""EMBEDDED"",
    CASE WHEN ""SIMS"" = 1 THEN 'Y' ELSE 'N' END AS ""SIMS"",
    CASE WHEN ""VOP"" = 1 THEN 'Y' ELSE 'N' END AS ""VOP"",
    CASE WHEN ""Slot punch"" = 1 THEN 'Y' ELSE 'N' END AS ""Slot punch"",
    CASE WHEN ""B2IT"" = 1 THEN 'Y' ELSE 'N' END AS ""B2IT"",
    CASE WHEN ""3 Roll Printing"" = 1 THEN 'Y' ELSE 'N' END AS ""3 Roll Printing"",
    CASE WHEN ""Cap Plating"" = 1 THEN 'Y' ELSE 'N' END AS ""Cap Plating"",
    CASE WHEN ""Perimeter"" = 1 THEN 'Y' ELSE 'N' END AS ""Perimeter"",
    CASE WHEN ""SB"" = 1 THEN 'Y' ELSE 'N' END AS ""SB""
FROM aaa
");
                //Console.WriteLine(sSQL.ToString());

                return Data.Get("ERP", sSQL.ToString()).Tables[0];
            });

            

            // 쿼리3
            // GROUP 3 : BOM_LIST
            var Task3 = Task.Run(() =>
            {
                var sSQL = new StringBuilder();
                sSQL.Append($@"
with BOM_LIST as (
    -- CCL, PPG, CF 나오는 spec_bom
    SELECT  A.bom_header_id, A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, 
            B.SEQ, 
            B.LAYER_CODE, -- 레이어 종류. 자재 종류  
            B.COMPONENT_ITEM_ID, B.OPERATION_SEQ, B.QUANTITY, B.THICK, 
            C.segment1 as COMPONENT_ITEM_CODE, C.description as COMPONENT_ITEM_NAME,
            (   SELECT  ccl.description -- 자재 타입 
                FROM   cbst_common_l      ccl
                WHERE  ccl.lookup_type  = 'Layer type'
                AND    ccl.enabled_flag = 'Y'
                AND    ccl.line_code    = B.LAYER_CODE --CBST_SPEC_BOM_L.layer_code
            ) as LAYER_TYPE
    FROM    cbst_spec_bom_h A 
            left outer join 
            cbst_spec_bom_l B
            on A.organization_id = B.organization_id
            and A.bom_header_id = B.bom_header_id
            left outer join 
            mtl_system_items_b C
            on A.organization_id = C.organization_id
            and A.bom_header_id = B.bom_header_id
            and B.COMPONENT_ITEM_ID = C.inventory_item_id
    WHERE   1 = 1
    and A.organization_id = 101
    --AND item_code = 'MCP20609H00' 
	--AND item_code LIKE '%333%' 
    --and A.revision ='000'
)
,
OPER_SEQ_LAYER_ITEM as (
    select  A.BOM_HEADER_ID, A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, A.operation_seq, A.LAYER_TYPE, A.COMPONENT_ITEM_ID, A.thick,
            rank() over (   partition by BOM_HEADER_ID, A.LAYER_TYPE 
                            order by to_number(A.OPERATION_SEQ) desc,   --> 4/14 수정 
                                     A.thick desc, A.COMPONENT_ITEM_ID) as LAYER_RANK
        from BOM_LIST A
    group by A.BOM_HEADER_ID, ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, A.operation_seq, A.LAYER_TYPE, A.COMPONENT_ITEM_ID, A.thick
) 
,
AAA as (
    select A.BOM_HEADER_ID, A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, 
           A.LAYER_TYPE, A.LAYER_RANK, A.THICK, A.COMPONENT_ITEM_ID
    from OPER_SEQ_LAYER_ITEM A
    where layer_rank = 1  -- 1순위만 
)
, AAAA as (
    select A.BOM_HEADER_ID, A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, max(CCL_THICK) as CCL_THICK, max(PPG_THICK) as PPG_THICK,  max(CF_THICK) as CF_THICK,   --> 동일 공정 seq 두께 합산? max?
           max(subproduct_code) as subproduct_code
    from (
         select A.BOM_HEADER_ID, A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, 
                     case when A.LAYER_TYPE = 'CCL' then thick else 0 end as CCL_THICK, 
                     case when A.LAYER_TYPE = 'PPG' then thick else 0 end as PPG_THICK,
                     case when A.LAYER_TYPE = 'CF'  then thick else 0 end as CF_THICK, 
                     case when A.LAYER_TYPE = 'SUB-PRODUCT'  then A.COMPONENT_ITEM_ID else null end as subproduct_code
            from AAA A   
         ) A     
    group by A.BOM_HEADER_ID, A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, A.subproduct_code -- subproduct code 로 들어있음 이름 찾아야 함 
)
select 
    A.BOM_HEADER_ID, 
    A.ORGANIZATION_ID, 
    A.ITEM_CODE AS ""Item no"", 
    A.REVISION AS ""Rev"", 
    A.CCL_THICK AS ""CCL Thick"", 
    A.PPG_THICK AS ""PPG Thick"",  
    A.CF_THICK AS ""C/Foil Thick"", 
    A.subproduct_code,
    (    SELECT B.description -- , segment1  -- * 
        FROM   mtl_system_items_b B
        WHERE  B.organization_id = A.ORGANIZATION_ID --101 --cbst_spec_bom_l.organization_id
        AND    B.inventory_item_id = A.subproduct_code -- 53530 -- 1761928 --cbst_spec_bom_l.component_item_id
    ) as  ""Sub Product""
from AAAA A
where 
	1=1
--AND item_code = 'MCP20609H00' 
--	AND item_code LIKE '%333%' 
--	and revision = '000'
");
                if (terms["item_no"].Length > 0) // item_no 조건
                {
                    sSQL.Append($@"
AND item_code LIKE '%{terms["item_no"].AsString()}%'
");
                }


                //Console.WriteLine(sSQL.ToString());

                return Data.Get("ERP", sSQL.ToString()).Tables[0];
            });

            // GROUP 5 : ETC 그룹
//            var Task4 = Task.Run(() =>
//            {
//                var sSQL = new StringBuilder();
//            sSQL.Append($@"
//with 
//item_mst_lookup_VALUE_M as (--> SQL Server 테이블 생성함
//   {str_lookup_value.ToString()}
//) 
//, 
//APS_LOOKUP_GRP5 as (
//    select  LOOKUP_TYPE_CODE, 
//            SEGMENT1 as COLUMN_CANDIDATE_ID,
//            ATTRIBUTE01 as UI_COLUMN_ORDER, 
//            ATTRIBUTE02 as ITEM_MASTER_COLUMN_NAME,
//            ATTRIBUTE03 as Type_Group,
//            ATTRIBUTE04 as CAPA_GROUP_LV1_CODE,
//            ATTRIBUTE05 as CAPA_GROUP_LV2_CODE,
//            ATTRIBUTE06 as CAPA_GROUP_LV1_NAME,
//            ATTRIBUTE07 as CAPA_GROUP_LV2_NAME,
//            ATTRIBUTE08 as CAPA_GROUP_LV1_TYPE,
//            ATTRIBUTE09 as LINE_ID,
//            ATTRIBUTE10 as UI_VIEW_LEVEL,
//            ATTRIBUTE11 as SOURCE
//    from item_mst_lookup_VALUE_M
//    where LOOKUP_TYPE_CODE = 'ITEM_MASTER_UI_COLUMN_CANDIDATE'
//    and ATTRIBUTE03 = 'Group5_SPEC_CAPA1'
//--    and active_flag = 'Y'
//)
//, 
//ITEM_REV_LIST as (
//    -- 추후 목록 대체할 것  
//        select 101 ORGANIZATION_ID, 'MCP20609H00' item_code,	'000' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'FLC09562A00' item_code,	'010' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'FLC11817A00' item_code,	'007' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'MCP11995A00' item_code,	'028' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'MCP18825A00' item_code,	'015' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'MCP20035B00' item_code,	'008' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'MCP20064A00' item_code,	'010' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'MCP20229A00' item_code,	'010' revision from dual union all 
//        select 101 ORGANIZATION_ID, 'MCP22377A00' item_code,	'010' revision from dual
//)
//--select * from ITEM_REV_LIST;
//, 
//ITEM_GROUP5_LIST as (
//    select  A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, 
//            B.CAPA_GROUP_LV1_CODE, B.CAPA_GROUP_LV2_CODE, 
//            B.ITEM_MASTER_COLUMN_NAME, B.CAPA_GROUP_LV1_NAME, B.CAPA_GROUP_LV2_NAME 
//    from ITEM_REV_LIST A
//         cross join 
//         APS_LOOKUP_GRP5 B  -- 이걸 기준으로 전체 컬럼 생성? 
//    where 
//");
//                if (terms["item_no"].Length > 0) // item_no 조건
//                {
//                    sSQL.Append($@"
//AND A.ITEM_CODE LIKE  '%{terms["item_no"].AsString()}%'
//");
//                }

//                sSQL.Append($@"
//    -- a.item_code = 'MCP20609H00' 
//    -- and a.revision = '000' 
//) --select * from ITEM_GROUP5_LIST A; --order by A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, A.ITEM_MASTER_COLUMN_NAME, A.CAPA_GROUP_LV1_NAME, A.CAPA_GROUP_LV2_NAME; 
//, 
//RESULT1 as (
//    select  A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, 
//            A.ITEM_MASTER_COLUMN_NAME, A.CAPA_GROUP_LV1_NAME, A.CAPA_GROUP_LV2_NAME, 
//            max(csc1.SPEC_VALUE) as SPEC_VALUE  -- 표시값 --> 여러 건 나오는 경우가 있어서 문의한 상태. 일단 Max 처리함.  ##############################
//         from ITEM_GROUP5_LIST A
//              left outer join 
//              cbst_spec_capa1 csc1
//         on  A.ORGANIZATION_ID = csc1.ORGANIZATION_ID
//         and A.ITEM_CODE = csc1.ITEM_CODE
//         and A.REVISION  = csc1.REVISION
//         and A.CAPA_GROUP_LV1_CODE  = csc1.CAPA_GROUP_CODE 
//         and A.CAPA_GROUP_LV2_CODE  = csc1.LOV_CODE 
//    --    and csc1.item_code = 'MCP20609H00' -- 여기에 조건 넣어야 룩업 항목 24개 다 나옴. 안 그러면 15건으로 줄어듦.  --> 전체 아이템에 대해 할 때는 이 부분 지워야 함.
//    --    and csc1.revision = '000'
//    --    and A.item_code = 'MCP20609H00' 
//    --    and A.revision = '000'
//    --where 1=1
//    --and capa_group_code = 20    -- Out Layer
//    --and lov_code = 70           -- Finger Width
//    --and lov_descrition = 'Trace Width'
//    --and csc1.item_code = 'MCP20609H00' 
//    --and csc1.revision = '000'
//    group by A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, 
//            A.ITEM_MASTER_COLUMN_NAME, A.CAPA_GROUP_LV1_NAME, A.CAPA_GROUP_LV2_NAME 
//),
//last_result as
//(select * 
//from ( select --A.*, 
//                A.ORGANIZATION_ID, A.ITEM_CODE, A.REVISION, A.ITEM_MASTER_COLUMN_NAME, A.SPEC_VALUE 
//            from result1 A
//         )
//    pivot (
//            max(SPEC_VALUE) 
//            for ITEM_MASTER_COLUMN_NAME in (  -- APS 룩업 컬럼명으로 표시하고 카운팅  
//                                -- select ATTRIBUTE02 as 'ITEM_MASTER_COLUMN_NAME' from item_mst_lookup_VALUE_M where attribute03 = 'Group5_SPEC_CAPA1' order by to_number(SEGMENT1);
//                                'Surface Cu' AS ""Surface Cu"",
//                                'Dimple' AS ""Dimple"",
//                                'Finger Width' AS ""Finger Width"",
//                                'Finger Space' AS ""Finger Space"",
//                                'Trace Width' AS ""Trace Width"",
//                                'Trace Space' AS ""Trace Space"",
//                                'Trace Pitch' AS ""Trace Pitch"",
//                                'Trace Delta' AS ""Trace Delta"",
//                                'Ball Pad Size' AS ""Ball Pad Size"",
//                                'Ball Open' AS ""Ball Open"",
//                                'Ball Clearance' AS ""Ball Clearance"",
//                                'Bump Pad' AS ""Bump Pad"",
//                                'Bump Pad Space' AS ""Bump Pad Space"",
//                                'Bump Pad Pitch' AS ""Bump Pad Pitch"",
//                                'SR Thick(TOP)' AS ""SR Thick(TOP)"",
//                                'SR Thick(BTM)' AS ""SR Thick(BTM)"",
//                                'SR Shift TOP' AS ""SR Shift TOP"",
//                                'SR Shift BTM' AS ""SR Shift BTM"",
//                                'Soft Ni' AS ""Soft Ni"",
//                                'Soft Au' AS ""Soft Au"",
//                                'Hard Ni' AS ""Hard Ni"",
//                                'Hard Au' AS ""Hard Au"",
//                                'Ni(Electroless)' AS ""Ni(Electroless)"",
//                                'HYNIX_Gv value' AS ""HYNIX_Gv value""

//                                )
//            )
//)
//SELECT 
//	ORGANIZATION_ID
//	,ITEM_CODE AS ""Item no""
//	,REVISION AS ""REV"",
//    ""Surface Cu"",
//    ""Dimple"",
//    ""Finger Width"",
//    ""Finger Space"",
//    ""Trace Width"",
//    ""Trace Space"",
//    ""Trace Pitch"",
//    ""Trace Delta"",
//    ""Ball Pad Size"",
//    ""Ball Open"",
//    ""Ball Clearance"",
//    ""Bump Pad"",
//    ""Bump Pad Space"",
//    ""Bump Pad Pitch"",
//    ""SR Thick(TOP)"",
//    ""SR Thick(BTM)"",
//    ""SR Shift TOP"",
//    ""SR Shift BTM"",
//    ""Soft Ni"",
//    ""Soft Au"",
//    ""Hard Ni"",
//    ""Hard Au"",
//    ""Ni(Electroless)"",
//    ""HYNIX_Gv value""
//FROM 
//	last_result
//");
//                // TODO : CAPA_GROUP_LV1_CODE 없는 이슈
//                Console.WriteLine(sSQL.ToString());

//                return Data.Get("ERP", sSQL.ToString()).Tables[0];
//            });

            var Task5 = Task.Run(() =>
            {
                var sSQL = new StringBuilder();
                sSQL.Append($@"
select
	ITEM_CODE AS ""Item no"",
	REV AS Rev,
	CUSTOMER AS ""Customer"",
	CATEGORY3 AS ""Category"", 
	ORDER_TYPE AS ""Order Type"",
	--CCL_THICK AS ""CCL Thick"",
	FINGER_PITCH AS ""Finger Pitch"",
	FINGER_DELTA AS ""Finger Delta"",
	TRACE_PITCH AS ""Trace Pitch"",
	TRACE_DELTA AS ""Trace Delta"",
	BBT_TYPE AS ""BBT TYPE""
from 
	TH_GUI_ITEM_MODEL_SEARCH
where
	1=1
");
                if (terms["division"].Length > 0) // division 조건
                {
                    sSQL.Append($@"
and DIVISION_ID = {terms["division"].V}
");
                }
                if (terms["item_no"].Length > 0) // item_no 조건
                {
                    sSQL.Append($@"
AND ITEM_CODE LIKE  '%{terms["item_no"].AsString()}%'
");
                }

                return Data.Get(sSQL.ToString()).Tables[0];
            });

            var Task6 = Task.Run( () =>
            {
                var sSQL = new StringBuilder();
                sSQL.Append($@"
select 
	ITEM_CODE AS ""Item no"",
	REVISION AS Rev,
	CATEGORY3 AS ""MFG Category"",
	CASE 
	    WHEN PATTERN_GUBUN = 'Tenting' THEN 'Tenting'
	    ELSE 'MSAP'
	END AS ""MSAP/Tenting"",
	PATTERN_GUBUN AS ""Pattern Type"",
	SHRINKAGE_RATE AS Yield,
	MAX_LOT_PNL AS ""Lot Size""
from 
	TH_GUI_ITEM_BY_PROCESS_GUBUN
where
    1=1
");
                if (terms["division"].Length > 0) // division 조건
                {
                    sSQL.Append($@"
and DIVISION_ID = {terms["division"].V}
");
                }

                if (terms["item_no"].Length > 0) // item_no 조건
                {
                    sSQL.Append($@"
AND ITEM_CODE LIKE  '%{terms["item_no"].AsString()}%'
");
                }

                return Data.Get(sSQL.ToString()).Tables[0];
            });

            var Task7 = Task.Run(() =>
            {
                var sSQL = new StringBuilder();
                sSQL.Append($@"
select 
	ITEM_CODE AS ""Item no""
	, REVISION AS Rev
	, JIG_CNT AS ""JIG 보유""
	, ROUND(TOT_JIG_CAPA_PCS, 0) AS ""JIG CAPA PCS/일""
from 
	TH_TAR_JIG_CAPA
WHERE
	ORGANIZATION_ID = 101
");

                if (terms["item_no"].Length > 0) // item_no 조건
                {
                    sSQL.Append($@"
AND ITEM_CODE LIKE  '%{terms["item_no"].AsString()}%'
");
                }

                return Data.Get(sSQL.ToString()).Tables[0];
            });

            // TODO : 리스트에 넣어서 어떤 조건에 들어가면 해당하는 Task만 돌아가도록 구현 필요
            var taskList = new List<Task<DataTable>>();

            taskList.Add(Task1);
            taskList.Add(Task2); // TODO : 현재 LINE_CD 이슈있음
            taskList.Add(Task3);
            //taskList.Add(Task4);
            taskList.Add(Task5);
            taskList.Add(Task6);
            taskList.Add(Task7);


            // 모든 쿼리가 실행 완료 되길 기다림 
            //Task.WaitAll(Task1, Task2, Task3, Task4, Task5, Task6);
            Task.WaitAll(taskList.ToArray());

            // 타임 아웃 적용시 
            //if(Task.WaitAll(new Task[] { Task1, Task2, Task3 }, 10000) == false) // 10초의 타임 아웃
            //    throw new Exception("지정된 시간이 초과 하였습니다.(10초)"); 

            DataTable[] resultTables = new[]
            {
                Task1.Result
                ,Task2.Result
                ,Task3.Result
                //,Task4.Result
                ,Task5.Result
                ,Task6.Result
                ,Task7.Result
            };

            // 4. 병합
            DataTable dtMain = resultTables[0].Copy();

            string[] keyColumns = new[] { "Item no", "Rev" };

            for (int i = 1; i < resultTables.Length; i++)
            {
                MergeDataTableByKeys(dtMain, resultTables[i], keyColumns);
            }

            return dtMain;
        }

        private DataTable SearchBasicHeaderColumn(Params data)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT 
	*
FROM
	LOOKUP_VALUE_M
WHERE
	1=1
	AND ACTIVE_FLAG = 'Y'
	AND LOOKUP_TYPE_CODE = 'ITEM_MASTER_UI_COLUMN_CANDIDATE'
    AND LOOKUP_TYPE_VERSION = (SELECT LOOKUP_TYPE_VERSION
								FROM [dbo].[LOOKUP_TYPE_M]
								where LOOKUP_TYPE_CODE	= 'ITEM_MASTER_UI_COLUMN_CANDIDATE' 
								and ACTIVE_FLAG = 'Y')
    AND (
            ATTRIBUTE03 = 'Group1_BASIC_INFO'
");
            if (data["special_process"])
            {
                sSQL.Append($@"
    OR ATTRIBUTE03 = 'Group7_Special_Process'
");
            }

            if (data["etc"])
            {
                sSQL.Append($@"
    OR ATTRIBUTE03 IN (
		'Group 9 model Search Sales Order Approve'
		,'Group10_SPEC_CAPA1 Finger Pitch Delta'
		,'Group11_SPEC_CAPA1 Trace Pitch Delta'
		,'Group12'
		,'Group2_SPEC_CAPA2'
		,'Group3_BOM_FINAL_OPER'
		,'Group4_ROUTING_FINAL'
		,'Group5_SPEC_CAPA1'
		,'Group6_SPS_GRADE'
		,'Group8_INPUT'
	)
");
            }


             sSQL.Append($@"
        )
ORDER BY CAST(ATTRIBUTE01 AS INT)
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchHeaderColumn()
        {
            // 컬럼가져오는 쿼리로 변경 필요
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"
SELECT  
	LOOKUP_TYPE, 
	LINE_CODE, 
	LINE_ID, 
	DESCRIPTION, 
	-- distinct description -- distinct  LOOKUP_TYPE 
	case when LINE_CODE =  10 then  10 
		when LINE_CODE = 520 then  20
		when LINE_CODE = 510 then  30
		when LINE_CODE = 600 then  40
		when LINE_CODE = 590 then  50
		when LINE_CODE = 640 then  60
		when LINE_CODE = 620 then  70
		when LINE_CODE = 630 then  80
		when LINE_CODE = 610 then  90
		when LINE_CODE = 650 then 100
		when LINE_CODE = 660 then 110
		when LINE_CODE = 680 then 120
		when LINE_CODE =  80 then 130
		when LINE_CODE = 190 then 140
		when LINE_CODE = 440 then 150
		when LINE_CODE =  90 then 160
		when LINE_CODE = 470 then 170             
		when LINE_CODE =  60 then 180
		when LINE_CODE = 130 then 190
		when LINE_CODE = 160 then 200
		when LINE_CODE = 500 then 210
		when LINE_CODE = 180 then 220
		when LINE_CODE = 200 then 230
		when LINE_CODE = 210 then 240
		when LINE_CODE = 230 then 250
		when LINE_CODE = 860 then 260
		-- Singulation 없음 
		when LINE_CODE = 220 then 280
		when LINE_CODE = 700 then 290
		when LINE_CODE = 710 then 300
		when LINE_CODE = 690 then 310
		when LINE_CODE = 280 then 320
		when LINE_CODE = 290 then 330
		when LINE_CODE = 300 then 340
		when LINE_CODE = 310 then 350
		when LINE_CODE = 150 then 360
		when LINE_CODE = 270 then 370
		when LINE_CODE = 240 then 380
		when LINE_CODE = 250 then 390
		when LINE_CODE = 170 then 400
		when LINE_CODE = 120 then 410
		 -- when LINE_CODE =  then 420  --  ETP 없음 
		when LINE_CODE = 580 then 430
		when LINE_CODE =  70 then 440
		when LINE_CODE = 100 then 450
		when LINE_CODE = 110 then 460
		when LINE_CODE = 260 then 470
		when LINE_CODE = 140 then 480
		when LINE_CODE =  50 then 490
		when LINE_CODE =  40 then 500
	else 999
		end as sort_order
FROM   
	cbst_common_l ccl
WHERE  
	ccl.enabled_flag = 'Y'
	AND lookup_type = 'CAPA_Special process' 
ORDER BY SORT_ORDER
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 조회 로직 
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private Params search_chart(Params terms)
        {
            Params result = new();

            return result;
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
        private DataTable delete(ParamList data)
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

        /// <summary>
        /// 저장된 그리드 헤더컬럼 가져오기
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private DataTable SearchGrid(Params terms)
        {
            DTClient.UserInfoMerge(terms);

            string USER_ID = "admin";
            string GRID_ID = terms["grid_id"].AsString();

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
	SUG.COLUMN_NAME AS dataField,
    SUG.COLUMN_NAME AS label,
	SUG.VISIBLE AS visible,
	SUG.WIDTH AS width,
	SUG.FIX AS fixed,
	SUG.EDITABLE AS editable
FROM
	TH_GUI_USER_GRID SUG
WHERE
	1=1
	AND SUG.USER_ID = '{USER_ID}'
	AND SUG.GRID_ID = '{GRID_ID}'
ORDER BY SUG.COLUMN_ORDER
");
            _logger.LogInformation(">>> sql Query = {}", sSQL.ToString());

            return Data.Get(sSQL.ToString()).Tables[0];

        }

        private IActionResult OnPostPage(PostArgs e)
        {
            string command = e.Params["command"].AsString();
            _logger.LogInformation(">>> commond = {}", command);

            if (command == "open_routing")
            {
                string item_id = e.Params["item_id"].AsString();

                _logger.LogInformation(">>> item_id = {}", item_id);

            }
            
            
            
            return Page();
        }



        // baseTable에 extraTable 데이터를 merge
        //public static void MergeDataTableByKeys(DataTable baseTable, DataTable extraTable, string[] keyColumns)
        //{
        //    // 추가 컬럼이 있다면 baseTable에 컬럼 추가
        //    foreach (DataColumn col in extraTable.Columns)
        //    {
        //        if (!baseTable.Columns.Contains(col.ColumnName))
        //        {
        //            baseTable.Columns.Add(col.ColumnName, col.DataType);
        //        }
        //    }

        //    // extraTable의 행들을 key 기준으로 baseTable에 병합
        //    foreach (DataRow extraRow in extraTable.Rows)
        //    {
        //        // 필터 조건 문자열 생성
        //        string filter = string.Join(" AND ", keyColumns.Select(k =>
        //            $"[{k}] = '{extraRow[k].ToString().Replace("'", "''")}'"
        //        ));

        //        DataRow[] matches = baseTable.Select(filter);

        //        if (matches.Length > 0)
        //        {
        //            DataRow baseRow = matches[0];

        //            foreach (DataColumn col in extraTable.Columns)
        //            {
        //                if (!baseTable.Columns.Contains(col.ColumnName)) continue;
        //                if (keyColumns.Contains(col.ColumnName)) continue;

        //                baseRow[col.ColumnName] = extraRow[col.ColumnName];
        //            }
        //        }
        //    }
        //}

        public static void MergeDataTableByKeys(DataTable baseTable, DataTable extraTable, string[] keyColumns)
        {
            // 1. 컬럼 캐싱 및 추가
            var baseColumnNames = new HashSet<string>(baseTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
            foreach (DataColumn col in extraTable.Columns)
            {
                if (!baseColumnNames.Contains(col.ColumnName))
                {
                    baseTable.Columns.Add(col.ColumnName, col.DataType);
                    baseColumnNames.Add(col.ColumnName);
                }
            }

            // 2. 키 생성 함수 (null/공백 안전 처리)
            string GetKey(DataRow row) =>
                string.Join("|", keyColumns.Select(k =>
                {
                    var val = row[k];
                    if (val == null || val == DBNull.Value) return "";

                    string strVal = val.ToString().Trim();

                    // REVISION 컬럼이면 3자리로 패딩
                    if (k.Equals("Rev", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(strVal, out int revNum))
                        {
                            return revNum.ToString("D3"); // "1" → "001"
                        }
                    }

                    return strVal;
                }));

            // 3. baseTable 인덱싱
            var baseIndex = baseTable.AsEnumerable()
                .ToDictionary(row => GetKey(row), row => row);

            // 4. 병합
            foreach (DataRow extraRow in extraTable.Rows)
            {
                string key = GetKey(extraRow);
                if (baseIndex.TryGetValue(key, out DataRow baseRow))
                {
                    foreach (DataColumn col in extraTable.Columns)
                    {
                        string colName = col.ColumnName;
                        if (keyColumns.Contains(colName)) continue;

                        baseRow[colName] = extraRow[colName];
                    }
                }
            }
        }



    }
}
