using HS.Core;
using HS.Web.Common;
using HS.Web.Middleware;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace HS.Web.Logic
{
    [Obsolete("삭제예정")]
    public class DTWebSocket
    {
        /// <summary>
        /// 접속한 모든 클라이언트 브로드캐스팅 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clients"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void BroadCastingWarnVideo(string message, List<(string id, WebSocket websocket)> clients)
        {
            try
            {
                //➕ 로 쪼갠다 ASSETID➕LOG_CTRL SEQ
                string[] assetMessage = message.Split('➕');
                string asset_id = assetMessage[0];
                string log_ctrl_seq = assetMessage[1];


                // video url 조회
                StringBuilder sSQL = new StringBuilder();
                sSQL.Append($@"
SELECT 
    ASET.ASSET_ID
    , ASET.ASSET_NM
    , ASET.STREAM_URL 

    , CTRL.ALERT_DESC
    , DATE_FORMAT(CTRL.REG_DT, '%Y-%m-%d %H:%i:%s') AS REG_DT
FROM 
    DT_ASSET ASET
INNER JOIN ASSET_LOG_CTRL CTRL
    ON CTRL.ASSET_ID = ASET.ASSET_ID
WHERE ASET.CLIENT = '0100' AND ASET.ASSET_ID = '{asset_id}' AND CTRL.SEQ = '{log_ctrl_seq}'
");

                Params assetInfo = Data.Get(sSQL.ToString()).Tables[0].ToParams();

                if (assetInfo["STREAM_URL"].AsString() != "")
                {
                    WebSocketResponse res = new WebSocketResponse();
                    res.message = "위험영상";
                    res.data = new { status = "ok", message = "위험영상", desc = assetInfo["ALERT_DESC"].AsString(), regdt = assetInfo["REG_DT"].AsString(), assetnm = assetInfo["ASSET_NM"].AsString(), assetid = assetInfo["ASSET_ID"].AsString(), videourl = assetInfo["STREAM_URL"].AsString() };

                    string resString = JsonConvert.SerializeObject(res, Formatting.Indented);
                    var responseBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(resString));

                    clients.ForEach(async client =>
                    {
                        await client.websocket.SendAsync(responseBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    });
                }

                
            }
            catch (Exception ex)
            {
                WebSocketResponse res = new WebSocketResponse();
                res.message = "Error";
                res.data = new { status = "ng" , message = ex.Message };

                string resString = JsonConvert.SerializeObject(res, Formatting.Indented);
                var responseBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(resString));

                clients.ForEach(async client =>
                {
                    await client.websocket.SendAsync(responseBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                });
            }
        }
    }
}
