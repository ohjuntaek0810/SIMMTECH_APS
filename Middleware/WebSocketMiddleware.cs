using HS.Core;
using HS.Web.Logic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace HS.Web.Middleware
{
    [Obsolete("재정의요망")]
    public class WebSocketResponse
    {
        public string message { get; set; }
        public object data { get; set; }
    }

    [Obsolete("재정의요망")]
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        const int filebufferSize = 102400000;
        const int bufferSize = 4096;
        byte[] fileBuffer;
        string fileName;
        long totalBytes = 0; // 파일의 총 크기

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                if (webSocket.State != WebSocketState.Open)
                    return;

                bool isReceivingFile = false;

                var buffer = new ArraySegment<byte>(new byte[bufferSize]);
                var token = CancellationToken.None;

                while (webSocket.State == WebSocketState.Open)
                {
                    string path = context.Request.Path.Value;

                    if (path == "/dt/warn")
                    {
                        WebSocektAIControl.AI = webSocket;
                    }

                    if (path == "/dt/info") // dt 용 
                    {
                        if (WebSocektAIControl.DTClients.Any(A => A.websocket == webSocket) == false)
                        {
                            Guid guid = new Guid();
                            WebSocektAIControl.DTClients.Add((guid.ToString(), webSocket));
                        }
                    }

                    var received = await webSocket.ReceiveAsync(buffer, token);

                    // 아래 로직은 실서버에서 적용할것 
                    if (received.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, received.Count);
                        string response = "";

                        // 고승범 선임 신호 받을때 
                        if (path == "/dt/warn")
                        {
                            WebSocektAIControl.BroadCasting(message);
                        }

                        // 처음 접속시 
                        if (path == "/dt/info")
                        {
                            WebSocektAIControl.ReturnMessage(webSocket, message);
                        }
                    }
                    else if (received.MessageType == WebSocketMessageType.Binary)
                    {

                    }
                    else if (received.MessageType == WebSocketMessageType.Close)
                    {
                        // AI 관제 웹소켓 끊김
                        if (WebSocektAIControl.AI == webSocket)
                            WebSocektAIControl.AI = null;

                        // DT Client 웹소켓 끊김
                        if (WebSocektAIControl.DTClients.Any(A => A.websocket == webSocket))
                        {
                            var client = WebSocektAIControl.DTClients.SingleOrDefault(S => S.websocket == webSocket);
                            if (client != (null, null))
                                WebSocektAIControl.DTClients.Remove((client.id, client.websocket));
                        }
                    }
                }
            }
            else
            {
                // 웹 소켓 요청이 아닌 경우 다음 미들웨어로 전달합니다.
                await _next(context);
            }
        }
    }
}
