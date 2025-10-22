using HS.Core;
using HS.Web.Common;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using Popbill.Message;
using System.Net.WebSockets;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace HS.Web.Logic
{
    [Obsolete("삭제예정")]
    public class DTResult
    { 
        public ParamList DangerArea { get; set; }
        public ParamList DangerMotion { get; set; }
        public ParamList FireAlarm { get; set; }
        public ParamList WaringVideo { get; set; }
    }

    public static class WebSocektAIControl
    {
        public static WebSocket? AI;

        public static List<(string id, WebSocket websocket)> DTClients;

        static WebSocektAIControl()
        {
            AI = null;
            DTClients = new List<(string id, WebSocket websocket)>();
        }

        public static void BroadCasting(string message)
        {
            var responseBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("OK"));

            if (AI != null)
            {
                AI.SendAsync(responseBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }

            Thread.Sleep(0);

            string resString = GetData(message);
            
            var responseBuffer2 = new ArraySegment<byte>(Encoding.UTF8.GetBytes(resString));

            DTClients.ForEach(async client =>
            {
                await client.websocket.SendAsync(responseBuffer2, WebSocketMessageType.Text, true, CancellationToken.None);
            });
        }

        public static void ReturnMessage(WebSocket webSocket, string message)
        {
            if(message.Equals("all"))
            {
                WebSocektAIControl.BroadCasting(message);
            }
            else
            {
                Thread.Sleep(0);
                string resString = GetData("");

                var responseBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(resString));
                webSocket.SendAsync(responseBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private static string GetData(string message)
        {
            DTResult result = new DTResult();

            // 실시간영상 URL 추후 작업 

            // 위험 지역
            StringBuilder sSQL = new StringBuilder();
            sSQL.Append($@"
SELECT 
	A.PP_ID AS AREA_ID
	, A.LEVEL_ID 
    , DATE_FORMAT(A.UPD_DT, '%Y-%m-%d %H:%i:%s.%f') AS UPT_DT

	, DT.PP_NM AS AREA_NM
FROM DT_DANGERAREA A
INNER JOIN DT_PP DT
	ON DT.PP_ID = A.PP_ID
WHERE 1 = 1
ORDER BY A.UPD_DT DESC, A.PP_ID;
");


            ParamList plResult1 = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            // 위험 행동
            sSQL.Clear();

            sSQL.Append($@"
SELECT 
    A.PP_ID AS AREA_ID
    , A.ALERT_DESC 
    , A.STATUS 
    , DATE_FORMAT(A.UPD_DT, '%Y-%m-%d %H:%i:%s.%f') AS UPT_DT

    , DT.TO_DT_ALARM 
	, DT.PP_NM AS AREA_NM
	
	, CLSF.HRF_CLSF_S_CD   		AS CLSF_CD
	, CLSF.HRF_CLSF_S_NM  		AS CLSF_NM
FROM DT_DANGERMOTION A
INNER JOIN DT_PP DT
	ON DT.PP_ID = A.PP_ID
INNER JOIN SI_HRF_CLSF CLSF
	ON CLSF.HRF_CLSF_S_CD = A.HRF_CLSF_S_CD
WHERE 1 = 1
-- AND A.UPD_DT >= DATE_ADD(NOW(), INTERVAL -5 MINUTE)
ORDER BY A.UPD_DT DESC, A.PP_ID;
");

            ParamList plResult2_temp = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            //            // 로그 남기기 
            //            IConfiguration Configuration = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();
            //            string logPath = Configuration.GetSection("HSLogPath").Value;
            //            string logJson = $@"
            //[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}]
            //{JsonConvert.SerializeObject(plResult2_temp, Formatting.Indented)}
            //";
            //            // 조회내용을 파일로 기록
            //            System.IO.File.AppendAllText(logPath, logJson);

            ParamList plResult2 = dtAlarm(plResult2_temp);

            // 화재 알람
            sSQL.Clear();

            sSQL.Append($@"
SELECT 
    A.PP_ID                 AS AREA_ID
    , A.ALERT_DESC
    , A.ALERT_DESC2         
    , A.FIRE_STATUS         AS STATUS
    , A.FIRE_STATUS2        AS STATUS2
    , DATE_FORMAT(A.UPD_DT, '%Y-%m-%d %H:%i:%s.%f') AS UPT_DT

    , DT.TO_DT_ALARM 
	, DT.PP_NM AS AREA_NM
FROM DT_FIREALARM  A
INNER JOIN DT_PP DT
	ON DT.PP_ID = A.PP_ID
WHERE 1 = 1
-- AND A.UPD_DT >= DATE_ADD(NOW(), INTERVAL -5 MINUTE)
ORDER BY A.UPD_DT DESC, A.PP_ID;
");

            ParamList plResult3_temp = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            ParamList plResult3 = dtAlarm(plResult3_temp, "FIRE");



            // 실시간 영상
            sSQL.Clear();

            sSQL.Append($@"
#위험행동
SELECT 
    A.PP_ID AS AREA_ID
	, DT.PP_NM AS AREA_NM
	
	, DT.STREAM_URL
FROM DT_DANGERMOTION A
INNER JOIN DT_PP DT
	ON DT.PP_ID = A.PP_ID
WHERE A.STATUS > 0
AND A.UPD_DT >= DATE_ADD(NOW(), INTERVAL -5 MINUTE)

UNION ALL

#화재상황
SELECT 
    A.PP_ID                 AS AREA_ID
	, DT.PP_NM AS AREA_NM
	
	, DT.STREAM_URL
FROM DT_FIREALARM  A
INNER JOIN DT_PP DT
	ON DT.PP_ID = A.PP_ID
WHERE (A.FIRE_STATUS > 0 OR A.FIRE_STATUS2 > 0)
AND A.UPD_DT >= DATE_ADD(NOW(), INTERVAL -5 MINUTE)
");

            ParamList plResult4 = Data.Get(sSQL.ToString()).Tables[0].ToParamList();

            result.DangerArea = plResult1;
            result.DangerMotion = plResult2;
            result.FireAlarm = plResult3;
            result.WaringVideo = plResult4;

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        private static ParamList dtAlarm(ParamList data, string type = "MOTION")
        {
            ParamList result = new ParamList();

            // TO_DT_ALARM을 분석.  TK001010FF0000
            Regex regex = new Regex(@"[0-9]{1,2}/((([A-Z]{2}[+]?)+/){1,4})([A-Z]{2}[0-9]{6}[A-Z0-9]{6})");
            data.ForEach(item =>
            {
                Params resultItem = new Params();

                resultItem["AREA_ID"] = item["AREA_ID"];
                resultItem["AREA_NM"] = item["AREA_NM"];
                resultItem["ALERT_DESC"] = item["ALERT_DESC"];
                resultItem["ALERT_DESC2"] = item["ALERT_DESC2"];
                resultItem["STATUS"] = item["STATUS"];
                resultItem["STATUS2"] = item["STATUS2"];
                resultItem["UPT_DT"] = item["UPT_DT"];
                resultItem["STREAM_URL"] = item["STREAM_URL"];
                resultItem["CLSF_CD"] = item["CLSF_CD"];
                resultItem["CLSF_NM"] = item["CLSF_NM"];

                if (type != "FIRE")
                {
                    resultItem.Remove("ALERT_DESC2");
                    resultItem.Remove("STATUS2");
                }

                string TO_DT_ALARM = item["TO_DT_ALARM"].AsString();

                if (regex.IsMatch(TO_DT_ALARM))
                {
                    var match = regex.Match(TO_DT_ALARM);

                    string? cameraZoom = match.Groups?[3]?.Value.ToString();
                    string? cameraTwinkle = match.Groups?[4]?.Value.ToString();

                    if (string.IsNullOrEmpty(cameraZoom))
                        resultItem["ZOOM"] = false;
                    else
                        resultItem["ZOOM"] = true;


                    // TK001010FF0000
                    if (string.IsNullOrEmpty(cameraTwinkle) == false && cameraTwinkle.Length == 14)
                    {
                        resultItem["TWINKLE"] = cameraTwinkle.Substring(0, 2) == "TK" ? true : false;
                        resultItem["INTERVAL"] = int.Parse(cameraTwinkle.Substring(2, 3));
                        resultItem["COUNT"] = int.Parse(cameraTwinkle.Substring(5, 3));
                        resultItem["COLOR"] = "#" + cameraTwinkle.Substring(8, 6);
                    }
                    else
                    {
                        resultItem["TWINKLE"] = false;
                        resultItem["INTERVAL"] = 0;
                        resultItem["COUNT"] = 0;
                        resultItem["COLOR"] = "";
                    }
                }

                result.Add(resultItem);
            });
            return result;
        }
    }
}
