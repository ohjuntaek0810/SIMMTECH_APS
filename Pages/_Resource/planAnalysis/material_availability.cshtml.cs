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
    public class material_availability : BasePageModel
    {
        public material_availability()
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
                toClient["fileName"] = this.SearchFileName(terms);
            }

            else if (e.Command == "search_detail")
            {
                Params terms = e.Params["terms"];

                toClient["data"] = this.SearchDetail(terms);
            }

            else if (e.Command == "save")
            {
                ParamList dataList = e.Params["data"];

                // 데이터 저장
                this.Save(dataList);


                // 데이터 저장
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

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
with 
-- 자재 재고
CUTOFF_MATERIAL_INVENTORY_ORG as  (
      SELECT  CUTOFF_DATE, REVISION, ITEM_CODE, ISNULL(A.TOTAL_QTY, 0)  AS TOTAL_QTY  -- 단위?
      FROM  TH_TAR_MATERIAL_INVENTORY_L A WITH(NOLOCK)  -- sum 완료? 
      WHERE CUTOFF_DATE = CONVERT(CHAR(8), GETDATE(), 112)  -- 원본 cutoff date 기준. 나중에 원복해야 함. #############
	  --WHERE CUTOFF_DATE = CONVERT(CHAR(8), '20250708', 112)   -- 임시
      AND   REVISION    = (
							  SELECT MAX(REVISION)
							  FROM  TH_TAR_MATERIAL_INVENTORY_L WITH(NOLOCK)
							  WHERE CUTOFF_DATE = CONVERT(CHAR(8), GETDATE(), 112)  -- 원본 cutoff date 기준. 나중에 원복해야 함. #############
							        --CUTOFF_DATE = CONVERT(CHAR(8), '20250708', 112)   -- 임시
                          )
), 
-- 투입대기 자재소요량 (확정수량)
READY_BY_INPUT_MATERIAL as (
	SELECT  A.PRODUCT_GROUP_CODE,
			A.COMPONET_ITEM_CODE,
		    A.COMPONENT_ITEM,
		    CASE WHEN (CAST(CEILING(SUM(A.CONFIRMED_QTY)) AS INT) % 2) = 0 THEN CEILING(SUM(A.CONFIRMED_QTY))
				 ELSE CEILING(SUM(A.CONFIRMED_QTY)) + 1
			END AS EXTENDED_QUANTITY
	FROM	TH_TAR_READY_BY_INPUT_MATERIAL_L A WITH(NOLOCK)  -- 단일 버전 데이터. 삭제 후 재업됨.
	GROUP BY  A.PRODUCT_GROUP_CODE,
			  A.COMPONET_ITEM_CODE,
			  A.COMPONENT_ITEM
), 
-- 미입 (자재 입고예정)
expected_in_stock as (
	-- 자재 코드, 일별 예상 입고 수량 
	SELECT	A.MATERIAL_CODE, MAX(A.PO_REMAIN_QTY) AS EXPECTED_IN_QTY
	FROM	TH_TAR_EXPECTED_IN_MATERIAL_L A
	GROUP BY A.MATERIAL_CODE 
), 
-- 샘플 자재 안전재고 수량
SAMPLE_SAFETY_STOCK_MAT as (
	select MATERIAL_CODE, MATERIAL_NAME, SAMPLE_SAFETY_STOCK_QTY from TH_TAR_SAMPLE_SAFETY_STOCK_MAT_L   where use_yn = 'Y' 
),
--------------------------
expected_in_stock_ORG as (
	-- 자재 코드, 일별 예상 입고 수량 
	SELECT	A.MATERIAL_CODE, A.EXPECTED_IN_DATE, 
			SUM(A.EXPECTED_IN_QTY) AS EXPECTED_IN_QTY
	FROM	TH_TAR_EXPECTED_IN_MATERIAL_L A
	GROUP BY A.MATERIAL_CODE, A.EXPECTED_IN_DATE 
) 
,
expected_in_stock_ORG2 as (
	select A.MATERIAL_CODE, format(A.EXPECTED_IN_DATE, 'MM/dd')+': '+convert(varchar, A.EXPECTED_IN_QTY)  as in_list
	from expected_in_stock_ORG A
) , 
expected_in_stock_TEXT_LIST as (
	select MATERIAL_CODE, string_agg(in_list, ', ')  as EXPECTED_IN_LIST
	from expected_in_stock_ORG2 
	group by MATERIAL_CODE
) 
--------------------------
SELECT  
        CASE WHEN A.PRODUCT_GROUP_CODE = 'S' THEN 'SPS' ELSE 'HDI' END AS ""GROUP"",
        A.COMPONET_ITEM_CODE,
        A.COMPONENT_ITEM,
        A.EXTENDED_QUANTITY                             AS '확정수량',   
        ISNULL(B.TOTAL_QTY,0)                           AS 'INVENTORY',
		(ISNULL(B.TOTAL_QTY, 0) - A.EXTENDED_QUANTITY)  AS '과부족',
		C.EXPECTED_IN_QTY								AS '미입',
		(ISNULL(B.TOTAL_QTY, 0) - A.EXTENDED_QUANTITY + C.EXPECTED_IN_QTY)  AS '입고 후 과부족', 
		D.SAMPLE_SAFETY_STOCK_QTY as '샘플 자재 안전재고',
		E.EXPECTED_IN_LIST as 'SCHEDULE', -- 입고 예정 정보 목록 나열 
		F.REMARK 
FROM  READY_BY_INPUT_MATERIAL A
      LEFT OUTER JOIN
      CUTOFF_MATERIAL_INVENTORY_ORG B
	  ON    A.COMPONET_ITEM_CODE  = B.ITEM_CODE
	  LEFT OUTER JOIN
	  expected_in_stock C
	  ON    A.COMPONET_ITEM_CODE  = C.MATERIAL_CODE
	  LEFT OUTER JOIN
	  SAMPLE_SAFETY_STOCK_MAT D
	  ON  A.COMPONET_ITEM_CODE  = D.MATERIAL_CODE
	  left outer join 
	  expected_in_stock_TEXT_LIST E 
	  on A.COMPONET_ITEM_CODE = E.MATERIAL_CODE
	  left outer join
	  TH_GUI_READY_BY_INPUT_MATERIAL_REMARK F
	  on A.COMPONET_ITEM_CODE = F.COMPONET_ITEM_CODE
where
    1=1
");
            if (terms["group_id"].Length > 0)
            {
                if (terms["group_id"].AsString() == "SPS")
                {
                    sSQL.Append($@"
    and A.PRODUCT_GROUP_CODE = 'S'
");
                } else if (terms["group_id"].AsString() == "HDI")
                {
                    sSQL.Append($@"
    and A.PRODUCT_GROUP_CODE = 'H'
");
                }
            }

            if (terms["componet_item_code"].Length > 0)
            {
                sSQL.Append($@"
    and A.COMPONET_ITEM_CODE LIKE '%{terms["componet_item_code"].AsString()}%'
");
            }

                sSQL.Append($@"
order by A.COMPONET_ITEM_CODE
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchDetail(Params terms)
        {

            DTClient.UserInfoMerge(terms);

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT  
    COMPONET_ITEM_CODE,
    ASSEMBLY_ITEM_CODE,
    CUSTOMER_NAME,
    MODEL_NAME,
    SUM(CONFIRMED_QTY) AS EXTENDED_QUANTITY,
    LAYER,
    PATTERN
FROM  
    TH_TAR_READY_BY_INPUT_MATERIAL_L
WHERE  
    1=1
	AND COMPONET_ITEM_CODE = {terms["COMPONET_ITEM_CODE"].V}
GROUP BY  
    COMPONET_ITEM_CODE,
    ASSEMBLY_ITEM_CODE,
    CUSTOMER_NAME,
    MODEL_NAME,
    LAYER,
    PATTERN
");


            return Data.Get(sSQL.ToString()).Tables[0];
        }

        private DataTable SearchFileName(Params terms)
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
	TOP 1
	*
from 
	TH_GUI_UPLOAD_FILE
where
	MENU_CODE = 'PA010204'
order by INSERT_DTTM DESC
");

            return Data.Get(sSQL.ToString()).Tables[0];
        }

        /// <summary>
        /// 저장 로직 
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Save(ParamList data)
        {
            HS.Web.Proc.TH_GUI_READY_BY_INPUT_MATERIAL_REMARK.Save(data);
        }
    }
}
