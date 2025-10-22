using HS.Core;

namespace HS.Web.Logic
{
    [Obsolete("삭제예정")]
    public class WebSocketUtil
    {
        public static Params GetParams(HttpRequest request)
        {
            Params result = new Params();

            try
            {
                foreach (var key in request.Form.Keys)
                {
                    result[key] = request.Form[key].ToString();
                }
            }
            catch (Exception) { }

            try
            {
                foreach (var key in request.Query.Keys)
                {
                    result[key] = request.Query[key].ToString();
                }
            }
            catch (Exception) { }

            return result;
        }
    }
}
