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

    public delegate void PushbulletEventHandler(PushbulletEventArgs e);

    public class PushbulletEventArgs : EventArgs
    {
        public string message { get; set; }

        public PushbulletEventArgs()
        {
        }

        public PushbulletEventArgs(string Message)
        {
            this.message = Message;
        }
    }

    public class PushEvents
    {
        private static CCriticalSection myCriticalSection = new CCriticalSection();
        private static CMutex myMutex = new CMutex();
        public static event PushbulletEventHandler onPushReceived;

        public void PushbulletMessage(string Message)
        {
            PushEvents.onPushReceived(new PushbulletEventArgs(Message));
        }
    }


    /// <summary>
    /// This Class is used to Send Pushes
    /// </summary>
    public class Pushbullet
    {
        public WebSocketClient wsc = new WebSocketClient();
        public WebSocketClient.WEBSOCKET_RESULT_CODES ret;
        public WebSocketClient.WEBSOCKET_PACKET_TYPES opcode;
        String DataToSend = "abc123";
        public byte[] SendData = new byte[6];
        public String strc = "abc123";
        WebSocketClient.WEBSOCKET_RESULT_CODES wrc;
        public byte[] ReceiveData;
        public static string Access_Code;
        public static string Sender_Email;
        private static CCriticalSection myCC = new CCriticalSection();

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Pushbullet()
        {
            ReceiveData = new byte[SendData.Length];
            wsc.Port = 443;
            wsc.SSL = true;
            wsc.SendCallBack = SendCallback;
            wsc.ReceiveCallBack = ReceiveCallback;
            SendData = System.Text.Encoding.ASCII.GetBytes(DataToSend);  
        }

        
        public short PushReceived(string Message)
        {
            PushEvents PE = new PushEvents();
            PE.PushbulletMessage(Message);
            return 1;
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

        public class Dismiss
        {
            public bool dismissed { get; set; }
        }               

        /// <summary>
        /// Set the Access Code
        /// </summary>
        /// <param name="access_code"></param>
        public void setAccessCode(string access_code)
        {
            Access_Code = access_code;
        }

        public void setSenderEmail(string sender_Email)
        {
            Sender_Email = sender_Email;
        }

        /// <summary>
        /// Connect to the Pushbullet Websocket to monitor for Pushes
        /// </summary>
        public void connect()
        {
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
            getUserInfo();
        }

        /// <summary>
        /// Disconnect from the Pushbullet Websocket
        /// </summary>
        public void Disconnect()
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
        public void sendNote(string Email, string Title, string Body)
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
        public void Send(string commandstring)
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
                    //ErrorLog.Notice(response.ContentString.ToString());
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

        public ushort dismissPush(string PushIden)
        {
            if (Access_Code != "")
            {
                string commandstring = "";
                HttpsClient client = new HttpsClient();
                client.PeerVerification = false;
                client.HostVerification = false;
                client.Verbose = false;

                HttpsClientRequest request = new HttpsClientRequest();
                HttpsClientResponse response;
                String url = "https://api.pushbullet.com/v2/pushes/" + PushIden;

                request.KeepAlive = true;
                request.Url.Parse(url);
                request.Header.SetHeaderValue("Content-Type", "application/json");
                request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post;
                request.Header.SetHeaderValue("Authorization", "Bearer " + Access_Code);

                Dismiss dismiss = new Dismiss
                {
                    dismissed = true
                };
                commandstring = JsonConvert.SerializeObject(dismiss, Formatting.Indented);
                request.ContentString = commandstring;
                response = client.Dispatch(request);
                
                if (response.Code >= 200 && response.Code < 300)
                {
                    return 1;
                }
                else
                {
                    ErrorLog.Notice("Error Dismissing - " + response.Code.ToString() + "\n");
                    return 0;
                }
            }
            else

                return 0;
        }

        public ushort deletePush(string PushIden)
        {
            if (Access_Code != "")
            {
                HttpsClient client = new HttpsClient();
                client.PeerVerification = false;
                client.HostVerification = false;
                client.Verbose = false;
                client.UserName = Access_Code;
                

                HttpsClientRequest request = new HttpsClientRequest();
                HttpsClientResponse response;
                //String url = "https://api.pushbullet.com/v2/pushes/" + PushIden;
                String url = "https://api-pushbullet-com-fqp420kzi8tw.runscope.net/v2/pushes/" + PushIden;

                //request.KeepAlive = true;
                request.Url.Parse(url);
                request.Header.SetHeaderValue("Content-Type", "application/json");
                //request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Delete;
                request.Header.SetHeaderValue("Authorization", "Bearer " + Access_Code);

                response = client.Dispatch(request);
                string s = response.ContentString.ToString();
                if (response.Code >= 200 && response.Code < 300)
                {
                    //ErrorLog.Notice("Deleted\n");
                    return 1;
                }
                else
                {
                    ErrorLog.Notice("Error Deleting - " + response.Code.ToString() + "\n");
                    return 0;
                }
            }
            else

                return 0;
        }

        /// <summary>
        /// This Method Will Retrieve all pushes since x.
        /// </summary>
        /// <returns></returns>
        public ushort getPush()
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
                    client.Abort();
                    if (response.Code >= 200 && response.Code < 300)
                    {
                        string s = response.ContentString.ToString();
                        //ErrorLog.Notice(s + "\n");
                        string[] words = s.Split(',');
                        string PushIden = "";
                        string PushTitle = "";
                        string PushMessage = "";
                        bool PushUnread = true;
                        bool SentMessage = false;
                        foreach (string word in words)
                        {
                            //ErrorLog.Notice(word + "\n");
                            if (word.Contains("\"iden\""))
                            {
                                PushIden = word.Substring(8, word.Length - 9);
                            }
                            if (word.Contains("title"))
                            {
                                PushTitle = word.Substring(9, word.Length - 10);
                                if (PushTitle.Contains("\""))
                                {
                                    PushTitle.Substring(0, PushTitle.Length - 1);
                                }
                            }
                            if (word.Contains("body"))
                            {
                                if (word.Contains("}"))
                                {
                                    PushMessage = word.Substring(8, word.Length - 10);
                                }
                                else
                                {
                                    PushMessage = word.Substring(8, word.Length - 9);
                                    if (PushMessage.Contains("\""))
                                    {
                                        PushMessage.Substring(0, PushMessage.Length - 1);
                                    }
                                }
                            }
                            if (word.Contains("dismissed"))
                            {
                                if (word.Contains("true"))
                                {
                                    PushUnread = false;
                                    continue;
                                }
                            }
                            if (word.Contains("sender_email") && word.Contains(Sender_Email))
                            {
                                SentMessage = true;
                            }
                            if (word.Contains("}"))
                            {
                                //TODO Trigger Event To Output String to S+
                                if (PushTitle != "" || PushMessage != "")
                                {
                                    if (PushUnread == true)
                                    {
                                        if (SentMessage != true)
                                        {
                                            PushReceived("CMD=" + PushTitle + "." + PushMessage + ";");
                                        }
                                        ushort result = dismissPush(PushIden);
                                    }                                    
                                }
                                PushIden = "";
                                PushTitle = "";
                                PushMessage = "";
                                PushUnread = false;
                            }
                        }
                        //ErrorLog.Notice(response.ContentString.ToString());
                        // A response code between 200 and 300 means it was successful.
                        return 1;
                    }
                    else
                    {
                        ErrorLog.Notice("Response Code = " + response.Code.ToString() + "\n");
                        // A reponse code outside this range means the server threw an error.
                        return 0;
                    }
                }
                catch (Exception e)
                {
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
        /// This Method Will Retrieve all pushes since x.
        /// </summary>
        /// <returns></returns>
        public ushort getUserInfo()
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
                String url = "https://api.pushbullet.com/v2/users/me";

                try
                {
                    myCC.Enter(); // Will not finish, until you have the Critical Section
                    request.KeepAlive = true;
                    request.Url.Parse(url);
                    request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Get;
                    request.Header.SetHeaderValue("Authorization", "Bearer " + Access_Code);
                    request.ContentString = commandstring;

                    // Dispatch will actually make the request with the server
                    response = client.Dispatch(request);
                    client.Abort();
                    if (response.Code >= 200 && response.Code < 300)
                    {
                        string s = response.ContentString.ToString();
                        //ErrorLog.Notice(s + "\n");
                        string[] words = s.Split(',');
                        string senderEmail;
                        foreach (string word in words)
                        {
                            if (word.Contains("\"email\""))
                            {
                                senderEmail = word.Substring(11, word.Length - 12);
                                setSenderEmail(senderEmail);
                            }
                        }
                        
                        //ErrorLog.Notice(response.ContentString.ToString());
                        // A response code between 200 and 300 means it was successful.
                        return 1;
                    }
                    else
                    {
                        ErrorLog.Notice("Response Code = " + response.Code.ToString() + "\n");
                        // A reponse code outside this range means the server threw an error.
                        return 0;
                    }
                }
                catch (Exception e)
                {
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
        /// The Following Three Methods are for the Websocket Listener
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public int SendCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
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
        public int ReceiveCallback(byte[] data, uint datalen, WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {
                string s = Encoding.UTF8.GetString(data, 0, data.Length);
                if (s.Contains("push"))
                {
                    getPush();

                }
                //ErrorLog.Notice(s);

            }
            catch (Exception e)
            {
                return -1;
            }
            return 0;
        }

        public void AsyncSendAndReceive()
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