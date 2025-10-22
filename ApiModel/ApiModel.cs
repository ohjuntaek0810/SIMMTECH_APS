using Newtonsoft.Json;
using System.ComponentModel;

namespace HS.Web.ApiModel
{
    public class Base<T>
    { 
        public string key { get; set; }

        public List<T> Data { get; set; }
    }

    [Obsolete("삭제예정")]
    // RabbitMQ용 받을때 
    public class MessageRequest
    {
        public string Message { get; set; }
    }
}
