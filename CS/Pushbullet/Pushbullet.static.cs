using System;
using System.Text;
using Crestron.SimplSharp;              // For Basic SIMPL# Classes
using Crestron.SimplSharp.Net.Https;    // For access to HTTPS
using Crestron.SimplSharp.Net;          // For access to HTTPS
using Crestron.SimplSharp.CrestronWebSocketClient;
using Newtonsoft.Json;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json.Linq;
#pragma warning disable 0168

namespace PushbulletProcessor
{
    public delegate ushort DelegateFn(string message);



    /// <summary>
    /// This Class is used to Send Pushes
    /// </summary>
    public static class Pushbullet
    {
        public static WebSocketClient wsc = null;
        public static WebSocketClient.WEBSOCKET_RESULT_CODES ret;
        public static WebSocketClient.WEBSOCKET_PACKET_TYPES opcode;
        private static String DataToSend = "abc123";
        public static byte[] SendData = null;
        public static String strc = "abc123";
        public static WebSocketClient.WEBSOCKET_RESULT_CODES wrc;
        public static byte[] ReceiveData;
        public static string Access_Code;
        private static CCriticalSection myCC = new CCriticalSection();

        
        public static DelegateFn StaticFn { get; set; }


        /// <summary>
        /// Default Constructor
        /// </summary>
        ///
        static Pushbullet()
        {
                     
        }

        public static void PushReceived(string message)
        {
            if (StaticFn != null)
                StaticFn(message);
            
            //OnPushReceived(Message, new EventArgs());
        }

        /// <summary>
        /// The following are classes for the sole purpose of Sending in JSON Format. 
        /// </summary>
        public class Note
        {
            public string type{get; set;}
            public string title { get; set; }
            public string body {get; set;}
            public string email { get; set; }
        }
                
        public class Link
        {
            public string Type = "link";
            public string Title = "";
            public string Body = "";
            public string Url = "";
        }

        public class MapAddress
        {
            public string Type = "address";
            public string Name = "";
            public string Address = "";
        }

        public class Checklist
        {
            public string Type = "list";
            public string Title = "";
            public string Items = "";
        }

        public class File
        {
            public string Type = "file";
            public string File_Name = "";
            public string File_Type = "";
            public string File_url = "";
            public string Body = "";
        }

        /// <summary>
        /// Set the Access Code
        /// </summary>
        /// <param name="access_code"></param>
        public static void setAccessCode(string access_code)
        {
            Access_Code = access_code;
        }

        /// <summary>
        /// Connect to the Pushbullet Websocket to monitor for Pushes
        /// </summary>
        public static void connect()
        {
            wsc.Port = 443;
            wsc.SSL = true;
            wsc.SendCallBack = SendCallback;
            wsc.ReceiveCallBack = ReceiveCallback;
            SendData = System.Text.Encoding.ASCII.GetBytes(DataToSend);
            wsc.URL = "wss://stream.pushbullet.com/websocket/" + Access_Code;
            wrc = wsc.Connect();
            if (wrc == (int)WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                CrestronConsole.PrintLine("Websocket connected \r\n");
            }
            else
            {
                CrestronConsole.Print("Websocket could not connect to server.  Connect return code: " + wrc.ToString());
            }
        }

        /// <summary>
        /// Disconnect from the Pushbullet Websocket
        /// </summary>
        public static void Disconnect()
        {
            wsc.Disconnect();
            CrestronConsole.PrintLine("Websocket disconnected. \r\n");
        }

        /// <summary>
        /// Send a Push with the type of Note.
        /// </summary>
        /// <param name="Email"></param>
        /// <param name="Title"></param>
        /// <param name="Body"></param>
        public static void sendNote(string Email, string Title, string Body)
        {
            string commandstring = "";
            if (Access_Code != "")
            {
                Note note = new Note
                {
                    type = "note",
                    title = Title,
                    body = Body,
                    email = Email
                };
                commandstring = JsonConvert.SerializeObject(note, Formatting.Indented);
                Send(commandstring);
            }
        }


        /// <summary>
        /// This is what actually send the push
        /// </summary>
        /// <param name="commandstring"></param>
        public static void Send(string commandstring)
        {
            HttpsClient client = new HttpsClient();
            client.PeerVerification = false;
            client.HostVerification = false;
            client.Verbose = false;

            HttpsClientRequest request = new HttpsClientRequest();
            HttpsClientResponse response;
            String url = "https://api.pushbullet.com/v2/pushes";

            try
            {
                request.KeepAlive = true;
                request.Url.Parse(url);
                client.UserName = Access_Code;
                request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post;
                request.Header.SetHeaderValue("Content-Type", "application/json");
                request.Header.SetHeaderValue("Authorization", "Bearer " + Access_Code);
                request.ContentString = commandstring;
                response = client.Dispatch(request);

                if (response.Code >= 200 && response.Code < 300)
                {
                    // A response code between 200 and 300 means it was successful.
                    ErrorLog.Notice(response.ContentString.ToString());
                }
                else
                {
                    // A reponse code outside this range means the server threw an error.
                    ErrorLog.Notice("Pushbullet https response code: " + response.Code);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Exception in Pushbullet: " + e.ToString());
            }
        }

        public static void deletePush()
        {
            if (Access_Code != "")
            {
                HttpsClient client = new HttpsClient();
                client.PeerVerification = false;
                client.HostVerification = false;
                client.Verbose = false;

                HttpsClientRequest request = new HttpsClientRequest();
                HttpsClientResponse response;
                String url = "https://api.pushbullet.com/v2/pushes";

                request.KeepAlive = true;
                request.Url.Parse(url);
                request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Delete;
                request.Header.SetHeaderValue("Authorization", "Bearer " + Access_Code);
                response = client.Dispatch(request);
                client.Abort();
            }
        }

        /// <summary>
        /// This Method Will Retrieve all pushes since x.
        /// </summary>
        /// <returns></returns>
        public static ushort getPush()
        {            
            string commandstring = "";
            if (Access_Code != "")
            {
                HttpsClient client = new HttpsClient();
                client.PeerVerification = false;
                client.HostVerification = false;
                client.Verbose = false;

                HttpsClientRequest request = new HttpsClientRequest();
                HttpsClientResponse response;
                String url = "https://api.pushbullet.com/v2/pushes";

                try
                {
                    myCC.Enter(); // Will not finish, until you have the Critical Section
                    request.KeepAlive = true;
                    request.Url.Parse(url);
                    request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Get;
                    request.Header.SetHeaderValue("Content-Type", "application/json");
                    request.Header.SetHeaderValue("Authorization", "Bearer " + Access_Code);
                    request.ContentString = commandstring;


                    // Dispatch will actually make the request with the server
                    response = client.Dispatch(request);

                    if (response.Code >= 200 && response.Code < 300)
                    {
                        client.Abort();
                        string s = response.ContentString.ToString();
                        string[] words = s.Split(',');
                        string PushIden = "";
                        string PushTitle = "";
                        string PushMessage = "";
                        foreach (string word in words)
                        {
                            //ErrorLog.Notice(word + "\n");
                            if (word.Contains("iden"))
                            {
                                PushIden = word.Substring(8, word.Length - 9);
                            }
                            if (word.Contains("title"))
                            {
                                PushTitle = word.Substring(9, word.Length - 10);
                            }
                            if (word.Contains("message"))
                            {
                                PushMessage = word.Substring(11, word.Length - 12);
                            }
                            if (word.Contains("dismissed"))
                            {
                                //TODO Trigger Event To Output String to S+

                                PushReceived("CMD=" + PushTitle + "." + PushMessage + ";");
                                //ErrorLog.Notice("TX = " + PushTitle + "." + PushMessage + ";");
                                //TODO Delete Push.IDEN
                                PushIden = "";
                                PushTitle = "";
                                PushMessage = "";

                            }
                        }
                        //ErrorLog.Notice(response.ContentString.ToString());
                        // A response code between 200 and 300 means it was successful.
                        return 1;
                    }
                    else
                    {
                        client.Abort();
                        ErrorLog.Notice("Response Code = " + response.Code.ToString() + "\n");
                        // A reponse code outside this range means the server threw an error.
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    client.Abort();
                    ErrorLog.Error("Exception in Pushbullet - GetInfo: " + e.ToString());
                    return 0;
                }
                finally
                {
                    myCC.Leave();
                }
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// The Following Three Methods are for the Websocket
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static int SendCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {
                ret = wsc.ReceiveAsync();
            }
            catch (Exception e)
            {
                return -1;
            }

            return 0;
        }
        public static int ReceiveCallback(byte[] data, uint datalen, WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {
                string s = Encoding.UTF8.GetString(data, 0, data.Length);
                if (s.Contains("push"))
                {
                    getPush();

                }

            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        public static void AsyncSendAndReceive()
        {
            try
            {
                wsc.SendAsync(SendData, (uint)SendData.Length, WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME, WebSocketClient.WEBSOCKET_PACKET_SEGMENT_CONTROL.WEBSOCKET_CLIENT_PACKET_END);
            }
            catch (Exception e)
            {
                Disconnect();
            }
        }
    }
}