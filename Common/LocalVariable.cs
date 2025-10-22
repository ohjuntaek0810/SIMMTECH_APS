using System.Collections.Concurrent;

namespace HS.Web.Common
{
    /// <summary>
    /// 프로젝트별 전역 변수 선언 클래스
    /// </summary>
    public static class LocalVariable
    {
        public static readonly string RabbitMQ_TO_MES_KEY = AppSettings.GetSingleValue("HSConfig:RabbitMQ_RoutingKey:TO_MES").ToString();
        public static readonly string RabbitMQ_TO_QMS_KEY = AppSettings.GetSingleValue("HSConfig:RabbitMQ_RoutingKey:TO_QMS").ToString();
        public static readonly string RabbitMQ_TO_CMMS_KEY = AppSettings.GetSingleValue("HSConfig:RabbitMQ_RoutingKey:TO_CMMS").ToString();

        // 초기화 
        static LocalVariable()
        {

        }
    }
}
