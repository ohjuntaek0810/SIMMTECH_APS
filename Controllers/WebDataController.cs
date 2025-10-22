using HS.Core;
using HS.Web.Common;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI;
using System.Text;

namespace HS.Web.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class WebDataController : BaseController
    {
        #region System Info

        /// <summary>
        /// 상위메뉴
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("UP_MENU_CD")]
        public IActionResult UP_MENU_CD()
        {
            string CLIENT = this.Params["CLIENT"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"SELECT MENU_CD AS UP_MENU_CD , MENU_NM AS  UP_MENU_NM FROM TH_GUI_MENU  WHERE CMP_CD = '{CLIENT}' AND USE_YN = 'Y' ORDER BY MENU_CD").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        /// <summary>
        /// 로그인 권한 정보
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("LOGIN_GRP_CD")]
        public IActionResult LOGIN_GRP_CD()
        {
            string CLIENT = this.Params["CLIENT"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"SELECT GRP_ID AS LOGIN_GRP_CD, GRP_NM AS LOGIN_GRP_NM FROM TH_GUI_GRP  WHERE CMP_CD = '{CLIENT}' ORDER BY GRP_ID").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        /// <summary>
        /// 공통코드상위그룹
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("UP_GRP_CD")]
        public IActionResult UP_GRP_CD()
        {
            string CLIENT = this.Params["CLIENT"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"SELECT GRP_CD AS UP_GRP_CD , GRP_NM AS  UP_GRP_NM FROM SI_CODE_GROUP WHERE CMP_CD = '{CLIENT}' AND USE_YN = 'Y' ORDER BY GRP_CD").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        /// <summary>
        /// 공통코드그룹
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GRP_CD")]
        public IActionResult GRP_CD()
        {
            string CLIENT = this.Params["CLIENT"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"SELECT GRP_CD AS GRP_CD , GRP_NM AS  GRP_NM FROM SI_CODE_GROUP WHERE CMP_CD = '{CLIENT}' AND USE_YN = 'Y' ORDER BY GRP_CD").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        #endregion

        #region Basic Info

        /// <summary>
        /// 부서정보
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DPT_CD")]
        public IActionResult DPT_CD()
        {
            string CLIENT = this.Params["CLIENT"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"SELECT DPT_CD AS DPT_CD, DPT_NM AS DPT_NM FROM BI_DPT  WHERE CMP_CD = '{CLIENT}' ORDER BY DPT_CD").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        #endregion

        /// <summary>
        /// 연도
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("YEAR")]
        public IActionResult YEAR()
        {
            string CLIENT = this.Params["CLIENT"];
            bool all = this.Params["ALL"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                StringBuilder query = new StringBuilder();

                if (all)
                {
                    query.AppendLine("SELECT 'ALL' AS CMN_CD, '전체' AS CMN_NM, 9999 AS SEQ UNION ALL");
                }

                query.AppendLine($@"
SELECT 
     CMN_CD AS V 
    ,CMN_NM AS T 
    ,CMN_CD AS SEQ
FROM SI_CODE_INFO
WHERE   CMP_CD = '{CLIENT}' 
    AND GRP_CD = 'YEAR' 
    AND USE_YN = 'Y' 
ORDER BY SEQ DESC");

                ParamList result = Data.Get(query.ToString()).Tables[0].ToParamList(); ;

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        /// <summary>
        /// 월
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("MONTH")]
        public IActionResult MONTH()
        {
            string CLIENT = this.Params["CLIENT"];
            bool all = this.Params["ALL"];
            bool non = this.Params["NONE"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                StringBuilder query = new StringBuilder();

                if (all) query.AppendLine("SELECT 'ALL' AS V, '전체' AS T, 0 AS SEQ UNION ALL");
                if (non) query.AppendLine("SELECT ''  AS V, ''   AS T, 0 AS SEQ UNION ALL");

                query.AppendLine($@"
SELECT 
     CMN_CD AS V 
    ,CMN_NM AS T 
    ,SEQ    AS SEQ
FROM SI_CODE_INFO
WHERE   CMP_CD = '{CLIENT}' 
    AND GRP_CD = 'MONTH' 
    AND USE_YN = 'Y' 
ORDER BY SEQ");

                ParamList result = Data.Get(query.ToString()).Tables[0].ToParamList(); ;

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        /// <summary>
        /// 사업장
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DIV_CD")]
        public IActionResult DIV_CD()
        {
            ParamList result = new ParamList(@"
[
    { DIV_CD : ""ALL"", DIV_NM : ""전체""}
    , { DIV_CD : ""01"", DIV_NM : ""사업장1""}
    , { DIV_CD : ""02"", DIV_NM : ""사업장2""}
]
");
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// Y/N 콤보박스
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("yn")]
        public IActionResult yn()
        {
            ParamList result = new ParamList(@"
[
    { value : ""Y"", text : ""Y""}
    , { value : ""N"", text : ""N""}
]
");
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// 시간(HH)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("TIME_HH")]
        public IActionResult TIME_HH()
        {
            ParamList result = new ParamList();

            for (int i = 0; i < 24; i++)
            {
                Params time = new Params();

                time["HH"] = i.ToString("D2");
                time["HH_NM"] = i.ToString("D2") + "시";

                result.Add(time);
            }

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// 시간(MM)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("TIME_MM")]
        public IActionResult TIME_MM()
        {
            ParamList result = new ParamList(@"
[
    { MM : ""00"", MM_NM : ""00분"" }
    , { MM : ""10"", MM_NM : ""10분"" }
    , { MM : ""20"", MM_NM : ""20분"" }
    , { MM : ""30"", MM_NM : ""30분"" }
    , { MM : ""40"", MM_NM : ""40분"" }
    , { MM : ""50"", MM_NM : ""50분"" }
]
");
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// 상위공통코드
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("UP_CMN_CD")]
        public IActionResult UP_CMN_CD()
        {
            string CLIENT = this.Params["CLIENT"];
            string grp_cd = this.Params["GRP_CD"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"
SELECT 
     CMN_CD AS UP_CMN_CD 
    ,CMN_NM AS UP_CMN_NM 
FROM SI_CODE_INFO
WHERE   CMP_CD = '{CLIENT}' 
    AND GRP_CD = '{grp_cd}' 
    AND USE_YN = 'Y' 
ORDER BY SEQ
").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        /// <summary>
        /// 공통코드
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("CMN_CD")]
        public IActionResult CMN_CD()
        {
            string CLIENT = this.Params["CLIENT"];
            string grp_cd = this.Params["GRP_CD"];
            bool all = this.Params["ALL"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                StringBuilder query = new StringBuilder();

                if (all)
                {
                    query.AppendLine("SELECT 'ALL' AS CMN_CD, '전체' AS CMN_NM, 0 AS SEQ UNION ALL");
                }

                query.AppendLine($@"
SELECT 
     CMN_CD AS CMN_CD 
    ,CMN_NM AS CMN_NM 
    ,SEQ    AS SEQ
FROM SI_CODE_INFO
WHERE   CMP_CD = '{CLIENT}' 
    AND GRP_CD = '{grp_cd}' 
    AND USE_YN = 'Y' 
ORDER BY SEQ");

                ParamList result = Data.Get(query.ToString()).Tables[0].ToParamList(); ;

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        /// <summary>
        /// 업체코드
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("CST_CD")]
        public IActionResult CST_CD()
        {
            string CLIENT = this.Params["CLIENT"];

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"
SELECT 
	 CMP_CD
	,CST_CD
    ,CST_NM
    ,$GetBizNo(CST_BIZ_NO) AS CST_BIZ_NO
    ,CST_CEO_NM
    ,CST_TYP
    ,CST_IND
    ,CST_TEL
    ,CST_FAX
    ,CST_EML
    ,CST_ADDR1
FROM BI_CST
WHERE   CMP_CD = '{CLIENT}' 
ORDER BY CST_CD
").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }

        [HttpGet]
        [Route("USER_ID")]
        public IActionResult USER_ID()
        {

            try
            {
                Vali vali = new Vali(this.Params);
                vali.Null("CLIENT", "CLIENT가 존재하지 않습니다.");
                vali.Done();

                ParamList result = Data.Get($@"SELECT USER_ID , USER_NM FROM TH_GUI_USER  WHERE USE_YN = 'Y' ORDER BY USER_ID").Tables[0].ToParamList();

                return Content(result.ToJson(), "application/json");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { status = "ng", message = ex.Message })
                {
                    StatusCode = 400
                };
            }
        }


        /// <summary>
        /// ITEM_CODE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ITEM_CODE")]
        public IActionResult ITEM_CODE()
        {
            ParamList result = Data.Get(@"
SELECT
	ITEM_ID AS ITEM_CODE
FROM
	TH_TAR_ITEM_MASTER WITH(NOLOCK)
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// ITEM_CODE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ITEM_CODE_FOR_ROUTING")]
        public IActionResult ITEM_CODE_FOR_ROUTING()
        {
            ParamList result = Data.Get(@"
SELECT 
	DISTINCT ITEM_CODE
FROM 
	TH_TAR_ROUTING_H
WHERE
	1=1
	AND USE_YN = 'Y'
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// WIP_ROUTE_GROUP
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("WIP_ROUTE_GROUP")]
        public IActionResult WIP_ROUTE_GROUP()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];
            string INOUT = this.Params["INOUT"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
SELECT 
	LOOKUP_TYPE_CODE, 
	SEGMENT1 as CODE, 
	ATTRIBUTE01 as NAME, 
	SORT_ORDER, 
	ATTRIBUTE02 AS DIVISION_ID
FROM [dbo].[LOOKUP_VALUE_M]
where   LOOKUP_TYPE_CODE	= 'WIP_ROUTE_GROUP' 
and LOOKUP_TYPE_VERSION = (		SELECT LOOKUP_TYPE_VERSION
								FROM [dbo].[LOOKUP_TYPE_M]
								where LOOKUP_TYPE_CODE	= 'WIP_ROUTE_GROUP' 
								and ACTIVE_FLAG = 'Y'
						  )
and ACTIVE_FLAG ='Y'
");
            if (DIVISION_ID != null)
            {
                sSQL.Append($@"
and ATTRIBUTE02 = '{DIVISION_ID}'
");
            }
            if (INOUT != null)
            {
                if (INOUT == "IN")
                {
                    sSQL.Append($@"
and ATTRIBUTE03 = 'Y'   
");
                }
                else
                {
                    sSQL.Append($@"
and ATTRIBUTE04 = 'Y'
");
                }

            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// APS_DEPT_GRP
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("APS_DEPT_GRP")]
        public IActionResult APS_DEPT_GRP()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
SELECT 
	LOOKUP_TYPE_CODE, 
	SEGMENT1 as CODE, 
	ATTRIBUTE01 as NAME, 
	SORT_ORDER, 
	ATTRIBUTE02 AS DIVISION_ID
FROM [dbo].[LOOKUP_VALUE_M]
where   LOOKUP_TYPE_CODE	= 'APS_DEPT_GRP' 
and LOOKUP_TYPE_VERSION = (		SELECT LOOKUP_TYPE_VERSION
								FROM [dbo].[LOOKUP_TYPE_M]
								where LOOKUP_TYPE_CODE	= 'APS_DEPT_GRP' 
								and ACTIVE_FLAG = 'Y'
						  )
and ACTIVE_FLAG ='Y'
");
            if (DIVISION_ID != null)
            {
                sSQL.Append($@"
and ATTRIBUTE02 = '{DIVISION_ID}'
");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }
        /// <summary>
        /// RESOURCE_CAPA_GROUP
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("RESOURCE_CAPA_GROUP")]
        public IActionResult RESOURCE_CAPA_GROUP()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append(@"
SELECT 
	LOOKUP_TYPE_CODE, 
	SEGMENT1 as CODE, 
	ATTRIBUTE01 as NAME, 
	SORT_ORDER, 
	ATTRIBUTE04 AS DIVISION_ID
FROM [dbo].[LOOKUP_VALUE_M]
where   LOOKUP_TYPE_CODE	= 'RESOURCE_CAPA_GROUP' 
and LOOKUP_TYPE_VERSION = (		SELECT LOOKUP_TYPE_VERSION
								FROM [dbo].[LOOKUP_TYPE_M]
								where LOOKUP_TYPE_CODE	= 'RESOURCE_CAPA_GROUP' 
								and ACTIVE_FLAG = 'Y'
						  )
and ACTIVE_FLAG ='Y'
");
            if (DIVISION_ID != null)
            {
                sSQL.Append($@"
and ATTRIBUTE04 = '{DIVISION_ID}'
");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// DIVISION_LIST
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DIVISION_LIST")]
        public IActionResult DIVISION_LIST()
        {
            ParamList result = Data.Get(@"
SELECT
	'' as CODE,
	'ALL' as NAME
UNION ALL
SELECT 
    SEGMENT1 as CODE,    
    ATTRIBUTE01 as NAME
FROM 
    LOOKUP_VALUE_M
WHERE 
    lookup_type_code = 'DIVISION_LIST'
    and lookup_type_version = (select LOOKUP_TYPE_VERSION from lookup_type_m where lookup_type_code = 'DIVISION_LIST' and active_flag = 'Y')
    and ACTIVE_FLAG = 'Y'
--    order by SORT_ORDER
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// DIVISION_LIST
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DIVISION_LIST_FOR_DEPARTMENT")]
        public IActionResult DIVISION_LIST_FOR_DEPARTMENT()
        {
            ParamList result = Data.Get(@"
SELECT
	'' as CODE,
	'ALL' as NAME
union
SELECT SEGMENT1 as CODE,     
       ATTRIBUTE01 as NAME
  FROM LOOKUP_VALUE_M
    where lookup_type_code = 'DIVISION_LIST'
    and lookup_type_version = (select LOOKUP_TYPE_VERSION from lookup_type_m where lookup_type_code = 'DIVISION_LIST' and active_flag = 'Y') 
    and ACTIVE_FLAG = 'Y' 
union
select 
	DISTINCT DIVISION_ID as CODE, 
	DIVISION_ID as NAME 
from 
	TH_TAR_DEPT_MASTER
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// DIVISION_LIST
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DIVISION_LIST_NOT_IN_ALL")]
        public IActionResult DIVISION_LIST_NOT_IN_ALL()
        {
            ParamList result = Data.Get(@"
select DIVISION_ID AS CODE, DIVISION_ID AS NAME from DIVISION_LIST_V
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// DEPARTMENT_CLASS
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DEPARTMENT_CLASS")]
        public IActionResult DEPARTMENT_CLASS()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.AppendLine($@"
SELECT  
	DISTINCT A.DEPARTMENT_CLASS_CODE AS CODE,
	A.DEPARTMENT_CLASS_NAME AS NAME
FROM
	[APS].[dbo].[TH_TAR_DEPT_MASTER] A WITH(NOLOCK)
WHERE
	1=1
");
            if (DIVISION_ID != null)
            {
                sSQL.AppendLine($@"
AND A.DIVISION_ID = '{DIVISION_ID}'
");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// DEPARTMENT_NAME
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("DEPARTMENT_NAME")]
        public IActionResult DEPARTMENT_NAME()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];
            string DEPARTMENT_CLASS_CODE = this.Params["DEPARTMENT_CLASS_CODE"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.AppendLine($@"
SELECT  
	DISTINCT A.DEPARTMENT_CODE AS CODE,
	A.DEPARTMENT_NAME AS NAME
FROM
	[APS].[dbo].[TH_TAR_DEPT_MASTER] A WITH(NOLOCK)
WHERE
	1=1	 
");
            if (DIVISION_ID != null)
            {
                sSQL.AppendLine($@"
    AND A.DIVISION_ID = '{DIVISION_ID}'
");
            }

            if (DEPARTMENT_CLASS_CODE != null)
            {
                sSQL.AppendLine($@"
    AND A.DEPARTMENT_CLASS_CODE = '{DEPARTMENT_CLASS_CODE}'
");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// SITE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("SITE")]
        public IActionResult SITE()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];
            string DEPARTMENT_CLASS_CODE = this.Params["DEPARTMENT_CLASS_CODE"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.AppendLine($@"
SELECT
	'' AS CODE,
	'ALL' AS NAME
union
SELECT 
	distinct A.SITE_ID AS CODE,
	A.SITE_ID AS NAME
FROM
	[APS].[dbo].[TH_TAR_DEPT_MASTER] A WITH(NOLOCK)
WHERE
	1=1
");
            if (DIVISION_ID != null)
            {
                sSQL.AppendLine($@"
    AND A.DIVISION_ID = '{DIVISION_ID}'
");
            }

            if (DEPARTMENT_CLASS_CODE != null)
            {
                sSQL.AppendLine($@"
    AND A.DEPARTMENT_CLASS_CODE = '{DEPARTMENT_CLASS_CODE}'
");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }


        // <summary>
        /// APS_RESOURCE_GROUP
        /// USE IN RESOURCE_MASTER
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("APS_RESOURCE_GROUP")]
        public IActionResult APS_RESOURCE_GROUP()
        {
            ParamList result = Data.Get(@"
select 
	RESOURCE_CAPA_GROUP_ID AS CODE,
	RESOURCE_CAPA_GROUP_NAME AS NAME
from 
	RESOURCE_CAPA_GROUP_V
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// PLAN_ID 
        /// Lot별 공정별 순서별에서 사용
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("PLAN_ID")]
        public IActionResult PLAN_ID()
        {
            ParamList result = Data.Get(@"
select 
	PLAN_ID, 
	PLAN_ATTB_1 as WIP_YYYYMMDD,  
	PLAN_ATTB_2 as WIP_SEQ
from  
	th_mst_plan with (nolock)
where
	1=1
	AND PLAN_START_DTTM > (SELECT DATEADD(""WEEK"", -4, GETDATE()))
	and IS_FINISHED = 'Y'
order by INSERT_DTTM desc
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// PLAN_ID 
        /// Lot별 공정별 순서별에서 사용
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("PLAN_DETAIL")]
        public IActionResult PLAN_DETAIL()
        {
            string PLAN_ID = this.Params["PLAN_ID"];

            ParamList result = Data.Get($@"
select 
	PLAN_ATTB_1 as WIP_YYYYMMDD,  
	PLAN_ATTB_2 as WIP_SEQ
from  
	th_mst_plan with (nolock)
where
	1=1
	AND PLAN_START_DTTM > (SELECT DATEADD(""WEEK"", -4, GETDATE()))
	and IS_FINISHED = 'Y'
    and PLAN_ID = '{PLAN_ID}'
order by INSERT_DTTM desc
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// REVISION
        /// BOM에서 ITEM_CODE먼저 입력하면 조회되는 REVISION만 가져오도록
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("REVISION")]
        public IActionResult REVISION()
        {

            string ITEM_CODE = this.Params["ITEM_CODE"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
    DISTINCT REVISION 
from 
    cbst_spec_bom_h 
where 
    organization_id = 101 
");

            if (ITEM_CODE != null)
            {
                sSQL.AppendLine($@"
    AND ITEM_CODE LIKE '%{ITEM_CODE}%'
");
            }

            sSQL.Append(@"
order by revision
");


            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// PATTERN TYPE
        /// RESOURCE MASTER에서 가져온 RESOURCE CAPA ID 값으로 조회 필요
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("PATTERN_TYPE")]
        public IActionResult PATTERN_TYPE()
        {

            string ITEM_CODE = this.Params["ITEM_CODE"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
    SEGMENT1 as NAME, 
    sort_order  -- 목록 정렬용
FROM
    LOOKUP_VALUE_M 
where 
    LOOKUP_TYPE_CODE = 'PATTERN_CU_TYPE_LIST'
    and ACTIVE_FLAG = 'Y'
    and LOOKUP_TYPE_VERSION = (  select LOOKUP_TYPE_VERSION
                                                       from LOOKUP_TYPE_M
                                                          where LOOKUP_TYPE_CODE = 'PATTERN_CU_TYPE_LIST'
                                                          and active_flag = 'Y' )
     and SEGMENT2 = (
                    select RESOURCE_CAPA_GROUP_ID
                    from th_tar_resource_master
                    where RESOURCE_CODE = 'S5080E0010')  --> 행 데이터에서 RESOURCE_CODE 추출하여 적용
");

            if (ITEM_CODE != null)
            {
                sSQL.AppendLine($@"
    AND ITEM_CODE LIKE '%{ITEM_CODE}%'
");
            }

            sSQL.Append(@"
order by sort_order 
");


            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }


        // <summary>
        /// RESOURCE NAME
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("RESOURCE_NAME")]
        public IActionResult RESOURCE_NAME()
        {

            string DIVISION_ID = this.Params["DIVISION_ID"];
            string DEPARTMENT_CLASS_CODE = this.Params["DEPARTMENT_CLASS_CODE"];
            string DEPT_CODE = this.Params["DEPT_CODE"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
	DISTINCT RESOURCE_NAME AS CODE
from 
	TH_TAR_RESOURCE_MASTER
where
    1=1
");

            if (DIVISION_ID != null)
            {
                sSQL.AppendLine($@"
    AND DIVISION_ID LIKE '%{DIVISION_ID}%'
");
            }

            if (DEPARTMENT_CLASS_CODE != null)
            {
                sSQL.AppendLine($@"
    AND DEPT_CLASS_CODE LIKE '%{DEPARTMENT_CLASS_CODE}%'
");
            }

            if (DEPT_CODE != null)
            {
                sSQL.AppendLine($@"
    AND DEPT_CODE LIKE '%{DEPT_CODE}%'
");
            }


            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        // <summary>
        /// RESOURCE_CODE NAME
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("RESOURCE_CODE")]
        public IActionResult RESOURCE_CODE()
        {

            string DIVISION_ID = this.Params["DIVISION_ID"];
            string DEPARTMENT_CLASS_CODE = this.Params["DEPARTMENT_CLASS_CODE"];
            string DEPT_CODE = this.Params["DEPT_CODE"];
            string RESOURCE_NAME = this.Params["RESOURCE_NAME"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
	DISTINCT RESOURCE_CODE AS CODE
from 
	TH_TAR_RESOURCE_MASTER
where
    1=1
");

            if (DIVISION_ID != null)
            {
                sSQL.AppendLine($@"
    AND DIVISION_ID LIKE '%{DIVISION_ID}%'
");
            }

            if (DEPARTMENT_CLASS_CODE != null)
            {
                sSQL.AppendLine($@"
    AND DEPT_CLASS_CODE LIKE '%{DEPARTMENT_CLASS_CODE}%'
");
            }

            if (DEPT_CODE != null)
            {
                sSQL.AppendLine($@"
    AND DEPT_CODE LIKE '%{DEPT_CODE}%'
");
            }

            if (RESOURCE_NAME != null)
            {
                sSQL.AppendLine($@"
    AND RESOURCE_NAME LIKE '%{RESOURCE_NAME}%'
");
            }


            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }


        // <summary>
        /// CATEGORY_LEVEL1
        /// 대분류 MSAP / TENTING .. 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("CATEGORY_LEVEL1")]
        public IActionResult CATEGORY_LEVEL1()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
SELECT
	'' as CODE,
	'ALL' as NAME
union
select 
	PATTERN_TYPE_L1 AS CODE,
    PATTERN_TYPE_L1 AS NAME
from  
	PATTER_TYPE_L1_LIST_V 
");

            if (DIVISION_ID != null)
            {
                sSQL.AppendLine($@"
    WHERE DIVISION_ID LIKE '%{DIVISION_ID}%'
");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }


        // <summary>
        /// CATEGORY_LEVEL2
        /// 대분류 MSAP / TENTING .. 상세
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("CATEGORY_LEVEL2")]
        public IActionResult CATEGORY_LEVEL2()
        {
            ParamList result = Data.Get(@"
select '' AS CODE, 'ALL' AS NAME
union
select 
	DISTINCT SEGMENT4 AS CODE,
	SEGMENT4 AS NAME
from 
	[dbo].[LOOKUP_VALUE_M]
WHERE 
	1=1
	AND LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY'
	AND LOOKUP_TYPE_VERSION = ( select MAX(LOOKUP_TYPE_VERSION)
								FROM [dbo].[LOOKUP_VALUE_M]
								WHERE LOOKUP_TYPE_CODE = 'WIP_STATUS_PATTERN_CATEGORY')
").Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }


        // <summary>
        /// RESOURCE_MES_CAPA
        /// 설비그룹 Capa -> CAPA GROUP 필터용
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("RESOURCE_MES_CAPA")]
        public IActionResult RESOURCE_MES_CAPA()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
    'ALL' AS NAME,
    '' AS CODE
union
select 
	DISTINCT RESOURCE_CAPA_GROUP_NAME AS NAME,
    RESOURCE_CAPA_GROUP_NAME AS CODE
from 
	APS_SITE_RESOURCE_MES_CAPA_V
WHERE
    1=1
");
            if (DIVISION_ID != null)
            {
                sSQL.Append($@"
    AND DIVISION_ID = '{DIVISION_ID}'
");
            }

            sSQL.Append(@"
ORDER BY CODE
");


            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }


        // <summary>
        /// OWN_OUT_GBN
        /// 설비그룹 Capa -> IN/OUT 필터용
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("OWN_OUT_GBN")]
        public IActionResult OWN_OUT_GBN()
        {
            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
select 
    'ALL' AS NAME,
    '' AS CODE
union
select 
	DISTINCT OWN_OUT_GBN AS NAME,
    OWN_OUT_GBN AS CODE
from 
	APS_SITE_RESOURCE_MES_CAPA_V
WHERE
    1=1
order by code
");


            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }



        // <summary>
        /// TARGET DEPARTMENT
        /// ITEM_CODE로 조회
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("TH_TAR_WIP_ROUTING_DEPARTMENT")]
        public IActionResult TH_TAR_WIP_ROUTING_DEPARTMENT()
        {
            string ITEM_CODE = this.Params["ITEM_CODE"];
            string REVISION = this.Params["REVISION"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@"
            SELECT  
                  OPERATION_SEQ ,
                  OPERATION_SEQ AS OPERATION_SEQ_NUM,
                  DEPARTMENT_CODE AS DEPT_CODE,
                  DEPARTMENT_NAME AS DEPT_NAME
             FROM TH_TAR_ROUTING_L 
            WHERE ITEM_CODE =  '{ITEM_CODE}'   
              AND REVISION = '{REVISION}' 
              AND DEPARTMENT_CODE  NOT IN ('V0001','V9999') -- 가상 투입 완성 제외 
            ORDER BY OPERATION_SEQ
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }



        [HttpGet]
        [Route("SITE_TH_TAR_DEPT_MASTER_WITH_NAME_V")]
        public IActionResult SITE_TH_TAR_DEPT_MASTER_WITH_NAME_V()
        {
            StringBuilder sSQL = new StringBuilder();
            string DEPT_CODE = this.Params["DEPT_CODE"];
            sSQL.Append($@"
-- 이 조건으로 지정 SITE 목록 조회. route_name 동일한 조건까지 추가. 
WITH 
DEPT_MASTER_USER_SELECT AS ( 
    SELECT  DIVISION_ID, DEPT_CODE, RESOURCE_CAPA_GROUP_ID, ROUTE_NAME
    FROM  TH_TAR_DEPT_MASTER_WITH_NAME_V C
    WHERE  DEPT_CODE = '{DEPT_CODE}'     --> 사용자가 선택한 ITEM 표준 ROUTING 중의 공정 순번의 DEPT_CODE 를 조건으로 지정. ######## 
) 
SELECT  A.DIVISION_ID, A.DEPT_CODE, A.RESOURCE_CAPA_GROUP_ID, A.ROUTE_NAME, B.SITE_ID, B.APS_RESOURCE_ID, B.APS_RESOURCE_NAME  
  FROM  DEPT_MASTER_USER_SELECT A
 INNER JOIN 
    APS_SITE_RESOURCE_LIST_V B
   ON A.DIVISION_ID = B.DIVISION_ID
  AND A.RESOURCE_CAPA_GROUP_ID = B.RESOURCE_CAPA_GROUP_ID
 INNER JOIN
TH_TAR_DEPT_MASTER_WITH_NAME_V C
    ON A.DIVISION_ID = C.DIVISION_ID 
   AND A.RESOURCE_CAPA_GROUP_ID = C.RESOURCE_CAPA_GROUP_ID
   AND A.ROUTE_NAME = C.ROUTE_NAME  -- ROUTE_NAME까지 동일한 SITE들을 나열. 
   AND B.DIVISION_ID = C.DIVISION_ID 
   AND B.SITE_ID = C.SITE_ID  -- CASE WHEN B.SITE_ID = 'OUTSOURCE' THEN B.SITE_ID  ELSE C.SITE_ID END 
 WHERE  B.OWN_OUT_GBN = 'SIMMTECH' 
 UNION ALL 
-- 외주는 DEPT MASTER에 없는 SITE 이므로 별도로 처리. APS_SITE_RESOURCE_LIST_V 에 있으면 추가. 
SELECT  A.DIVISION_ID, A.DEPT_CODE, A.RESOURCE_CAPA_GROUP_ID, A.ROUTE_NAME, B.SITE_ID, B.APS_RESOURCE_ID, B.APS_RESOURCE_NAME
 FROM  DEPT_MASTER_USER_SELECT A
 INNER JOIN 
    APS_SITE_RESOURCE_LIST_V B
  ON A.DIVISION_ID = B.DIVISION_ID
 AND A.RESOURCE_CAPA_GROUP_ID = B.RESOURCE_CAPA_GROUP_ID
WHERE  B.OWN_OUT_GBN = 'OUTSOURCE' 
;
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }
        /// <summary>
        /// WIP_ITEM_CODE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("WIP_ITEM_CODE")]
        public IActionResult WIP_ITEM_CODE()
        {
            StringBuilder sSQL = new StringBuilder();
            string DEPT_CODE = this.Params["DEPT_CODE"];
            sSQL.Append($@"            
                SELECT ITEM_CODE 
                  FROM TH_TAR_WIP
                GROUP BY ITEM_CODE
                ORDER BY ITEM_CODE
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// WIP_ITEM_CODE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("WIP_ITEM_CODE_REVISION")]
        public IActionResult WIP_ITEM_CODE_REVISION()
        {
            StringBuilder sSQL = new StringBuilder();
            string ITEM_CODE = this.Params["ITEM_CODE"];
            sSQL.Append($@"            
                SELECT REVISION
                  FROM TH_TAR_WIP
                 WHERE  ITEM_CODE = '{ITEM_CODE}' 
                GROUP BY REVISION
                ORDER BY REVISION
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// WIP_ITEM_CODE
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("PROBLEM_TYPE_GB")]
        public IActionResult PROBLEM_TYPE_GB()
        {
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
                  SELECT 'LATE' PROBLEM_TYPE, '납기보다 늦어짐' PROBLEM_TYPE_NM
                  UNION ALL   
                  SELECT 'UNPLAN' PROBLEM_TYPE ,'기준정보 누락' PROBLEM_TYPE_NM
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// APS_ENG_SITE_RESOURCE_LIST_V__CAPA_GROUP_ID
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("APS_ENG_SITE_RESOURCE_LIST_V__CAPA_GROUP_ID")]
        public IActionResult APS_ENG_SITE_RESOURCE_LIST_V__CAPA_GROUP_ID()
        {
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
	            SELECT 
			            DIVISION_ID,
                        RESOURCE_CAPA_GROUP_ID ,
			            RESOURCE_CAPA_GROUP_NAME ,
                        SITE_ID,
                        APS_RESOURCE_ID

	             FROM  APS_ENG_SITE_RESOURCE_LIST_V WITH (NOLOCK)
	            GROUP BY 
			        	DIVISION_ID,
                        RESOURCE_CAPA_GROUP_ID ,
			            RESOURCE_CAPA_GROUP_NAME ,
                        SITE_ID,
                        APS_RESOURCE_ID
                        
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }


        /// <summary>
        /// GROUP_GUBUN
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GROUP_GUBUN")]
        public IActionResult GROUP_GUBUN()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];   
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
                select 
	                DISTINCT GROUP_GUBUN_ID AS CODE,
	                GROUP_GUBUN_NAME AS NAME
                from  
	                OM_BOTTLE_NECK_LIST_V with (nolock)
                where
                    1=1
            ");

            if (DIVISION_ID != null)
            {
                sSQL.Append($@"
                    AND DIVISION_ID = '{DIVISION_ID}'
                ");
            }
            sSQL.Append($@"
                order by GROUP_GUBUN_ID
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }


        /// <summary>
        /// BOTTLE_NECK
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("BOTTLE_NECK")]
        public IActionResult BOTTLE_NECK()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];
            string GROUP_GUBUN_NAME = this.Params["GROUP_GUBUN_NAME"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@" 
                select 
	                DISTINCT BOTTLE_NECK_ID AS CODE,
	                BOTTLE_NECK_NAME AS NAME
                from  
	                OM_BOTTLE_NECK_LIST_V with (nolock)
                where 
                    1=1
            ");
            if (DIVISION_ID != null)
            {
                sSQL.Append($@"
                    AND DIVISION_ID = '{DIVISION_ID}'
                ");
            }

            if (GROUP_GUBUN_NAME != null)
            {
                sSQL.Append($@"
                    AND GROUP_GUBUN_ID = '{GROUP_GUBUN_NAME}'
                ");
            }
            sSQL.Append($@"
                order by BOTTLE_NECK_ID
            ");

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// APS_ENG_SITE_RESOURCE_LIST_V__CAPA_GROUP_ID
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("APS_ENG_SITE_RESOURCE_LIST_V__SITE_ID")]
        public IActionResult APS_ENG_SITE_RESOURCE_LIST_V__SITE_ID()
        {
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
	          SELECT SITE_ID  
                FROM APS_ENG_SITE_RESOURCE_LIST_V 
               GROUP BY SITE_ID            
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// APS_ENG_SITE_RESOURCE_LIST_V_PDATA
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("APS_ENG_SITE_RESOURCE_LIST_V_PDATALIST")]
        public IActionResult APS_ENG_SITE_RESOURCE_LIST_V_PDATALIST()
        {
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
                SELECT
                 DIVISION_ID
                ,APS_RESOURCE_ID
                ,RESOURCE_CAPA_GROUP_ID
                ,RESOURCE_CAPA_GROUP_NAME
                ,SITE_ID
                ,RESOURCE_LEVEL AS RESOURCE_CNT
                ,APPLY_TOTAL_SITE_RCG_CAPA_M2_DAY_29              
                FROM APS_ENG_SITE_RESOURCE_LIST_V             
            ");
            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// 주간/야간(DAY/NIGHT)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("OFF_SHIFT")]
        public IActionResult OFF_SHIFT()
        {
            ParamList result = new ParamList(@"
                [
                    { OFF_SHIFT : """", OFF_SHIFT_NM : """"}
                    , { OFF_SHIFT : ""DAY"", OFF_SHIFT_NM : ""주간""}
                    , { OFF_SHIFT : ""NIGHT"", OFF_SHIFT_NM : ""야간""}
                ]
                ");
            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// TH_TAR_ITEM_MASTER
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("TH_TAR_ITEM_MASTER")]
        public IActionResult TH_TAR_ITEM_MASTER()
        {
            //string DIVISION_ID = this.Params["DIVISION_ID"];
           // string GROUP_GUBUN_NAME = this.Params["GROUP_GUBUN_NAME"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@" 
              SELECT               
                    ITEM_ID AS ITEM_CODE,
	                A.* 
               FROM TH_TAR_ITEM_MASTER A
              WHERE 
                    1=1
            ");

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// APS_RESOURCE_NAME
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("APS_RESOURCE_NAME")]
        public IActionResult APS_RESOURCE_NAME()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];
            string RESOURCE_CAPA_GROUP_ID = this.Params["RESOURCE_CAPA_GROUP_ID"];
            // string GROUP_GUBUN_NAME = this.Params["GROUP_GUBUN_NAME"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@" 
                select 
                    APS_RESOURCE_ID AS CODE, 
                    APS_RESOURCE_NAME AS NAME
                from 
                    APS_ENG_SITE_RESOURCE_LIST_V
                where
                    1=1
            ");

            if(DIVISION_ID != null)
            {
                sSQL.Append($@"
                    and DIVISION_ID = '{DIVISION_ID}'
                ");
            }

            if (RESOURCE_CAPA_GROUP_ID != null)
            {
                sSQL.Append($@"
                    and RESOURCE_CAPA_GROUP_ID = '{RESOURCE_CAPA_GROUP_ID}'
                ");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// APS_RESOURCE_NAME
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("APS_RESOURCE_GROUP_V2")]
        public IActionResult APS_RESOURCE_GROUP_V2()
        {
            string DIVISION_ID = this.Params["DIVISION_ID"];

            StringBuilder sSQL = new StringBuilder();

            sSQL.Append($@" 
                select
                    DISTINCT RESOURCE_CAPA_GROUP_ID AS CODE,
                    RESOURCE_CAPA_GROUP_NAME AS NAME
                from
                    APS_ENG_SITE_RESOURCE_LIST_V
                where
                    1=1
            ");

            if (DIVISION_ID != null)
            {
                sSQL.Append($@"
                    and DIVISION_ID = '{DIVISION_ID}'
                ");
            }

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }




        /// <summary>
        /// ITEM_CUST__TAR_DEPT_MASTER_WITH_NAME_V
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ITEM_CUST__TAR_DEPT_MASTER_WITH_NAME_V")]
        public IActionResult ITEM_CUST__TAR_DEPT_MASTER_WITH_NAME_V()
        {
           // string DIVISION_ID = this.Params["DIVISION_ID"];

            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
 SELECT distinct B.RESOURCE_CAPA_GROUP_ID, B.RESOURCE_CAPA_GROUP_NAME  
   FROM	th_tar_routing_l A  with (nolock)
		left outer join
		TH_TAR_DEPT_MASTER_WITH_NAME_V B  with (NOLOCK) 
		ON A.DEPARTMENT_CODE = B.DEPT_CODE
  WHERE 1=1 -- A.item_Code = 'FLC11939A00' 
    AND A.revision = dbo.GET_ITEM_MAX_REV ('FLC11939A00')
    AND A.DEPARTMENT_CODE not in ('V0001', 'V9999') 
  ORDER BY RESOURCE_CAPA_GROUP_ID            
            ");
            //if (DIVISION_ID != null)
            //{
            //    sSQL.Append($@"
            //        and DIVISION_ID = '{DIVISION_ID}'
            //    ");
            //}

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }


        /// <summary>
        /// ITEM_CUST__APS_ENG_SITE_RESOURCE_LIST_V
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ITEM_CUST__APS_ENG_SITE_RESOURCE_LIST_V")]
        public IActionResult ITEM_CUST__APS_ENG_SITE_RESOURCE_LIST_V()
        {
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
 SELECT 
		  SITE_ID AS PREFER_SITE_ID,
		  APS_RESOURCE_ID AS PREFER_RESOURCE_ID,
		  APS_RESOURCE_NAME AS PREFER_RESOURCE_NAME
   FROM  APS_ENG_SITE_RESOURCE_LIST_V with (nolock)
  WHERE 1=1 --RESOURCE_CAPA_GROUP_ID = 'APS_RCG_0004'
  ;       
            ");
          

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }

        /// <summary>
        /// GET_AR_CUSTOMERS
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GET_AR_CUSTOMERS")]
        public IActionResult GET_AR_CUSTOMERS()
        {
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@" 
                SELECT CUSTOMER_ID,
                       CUSTOMER_NUMBER,
                       CUSTOMER_NAME 
                  FROM AR_CUSTOMERS WITH(NOLOCK)
                 ;       
            ");                          

            ParamList result = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            return Content(result.ToJson(), "application/json");
        }




       





    }


}

