using System;
using System.Text;
using Crestron.SimplSharp;              // For Basic SIMPL# Classes
using Crestron.SimplSharp.Net.Https;    // For access to HTTPS
using Crestron.SimplSharp.Net;          // For access to HTTPS

namespace PushbulletProcessor
{
    public class Pushbullet
    {

        public Pushbullet()
        { }

        public string str2JSON(string urlString)
        {
            urlString = urlString.StartsWith("?") ? urlString.Substring(1) : urlString;
            string resultString = "{";

            string[] urlKeyVal = urlString.Split('&');

            foreach (string kv in urlKeyVal)
            {
                string k = kv.Substring(0, kv.LastIndexOf('='));
                string v = kv.Substring(kv.LastIndexOf('=') + 1);
                resultString += "\"" + k + "\"" + ":" + "\"" + v + "\",";
            }
            resultString = resultString.Substring(0, resultString.Length - 1);
            resultString += "}";


            return resultString;
        }

        public void sendMessage(string access_code, string email, string message, string title)
        {
            if (access_code != "")
            {
                HttpsClient client = new HttpsClient();
                client.PeerVerification = false;
                client.HostVerification = false;
                client.Verbose = true;

                HttpsClientRequest request = new HttpsClientRequest();
                HttpsClientResponse response;
                String url = "https://api.pushbullet.com/v2/pushes";

                try
                {
                    request.KeepAlive = true;
                    request.Url.Parse(url);
                    client.UserName = access_code;
                    request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post;
                    request.Header.SetHeaderValue("Content-Type", "application/json");
                    //request.Header.SetHeaderValue("Authorization", "Bearer " + access_token);
                    request.ContentString = str2JSON("?type=note" + "&" + "title=" + title + "&" + "body=" + message + "&" + "email=" + email);
                    // Dispatch will actually make the request with the server
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
            else
            {
                ErrorLog.Notice("Access Code is Blank\n");
            }
        }


        public ushort getUserInfo(string access_code)
        {
            if (access_code != "")
            {
                HttpsClient client = new HttpsClient();
                client.PeerVerification = false;
                client.HostVerification = false;

                HttpsClientRequest request = new HttpsClientRequest();
                HttpsClientResponse response;
                String url = "https://api.pushbullet.com/v2/users/me";

                try
                {
                    request.KeepAlive = true;
                    request.Url.Parse(url);
                    request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Get;
                    request.Header.SetHeaderValue("Content-Type", "application/json");
                    request.Header.SetHeaderValue("Authorization", "Bearer " + access_code);
                    request.ContentString = "";


                    // Dispatch will actually make the request with the server
                    response = client.Dispatch(request);

                    if (response.Code >= 200 && response.Code < 300)
                    {
                        // A response code between 200 and 300 means it was successful.
                        return 1;
                    }
                    else
                    {
                        // A reponse code outside this range means the server threw an error.
                        return 0;
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Error("Exception in Pushbullet - GetInfo: " + e.ToString());
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }
}