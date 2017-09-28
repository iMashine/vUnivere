using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Newtonsoft.Json.Linq;
using Windows.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Text.RegularExpressions;

namespace myNstu
{
    class TCPClient
    {
        private StreamSocket clientSocket;
        private HostName serverHost;
        private bool connected = false;

        private static bool isAuth = false;
        private static bool isNews = false;

        private string _status;
        private string _msg;

        public TCPClient()
        {
            clientSocket = new StreamSocket();
        }

        public async Task connectWithServer(string hostName, string port)
        {
            if (connected)
            {
                _status += "Already connected";
                return;
            }

            try
            {
                _status = "Trying to connect ...";

                serverHost = new HostName(hostName);
                await clientSocket.ConnectAsync(serverHost, port);
                connected = true;
                _status = "Connection established";

            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _status += "Connect failed with error: " + exception.Message;

                clientSocket.Dispose();
                clientSocket = null;
            }
        }

        public async Task<bool> sendMSG(int _type, string email = null, string password = null)
        {
            if (!connected)
            {
                _status = "Must be connected to send!";
                return false;
            }
            try
            {

                JObject o1 = null;

                switch (_type)
                {
                    case 1:
                        {
                            isNews = false;
                            isAuth = true;
                            o1 = JObject.Parse(@"{'type': 'auth', 'username': '" + email + "','password': '" + password + "'}");
                            break;
                        }
                    case 2:
                        {
                            isAuth = false;
                            isNews = true;
                            o1 = JObject.Parse(@"{'type': 'get_news'}");
                            break;
                        }
                }

                _status = "Trying to send data ...";

                string dataToSend = o1.ToString();

                string sendData = dataToSend + Environment.NewLine;
                DataWriter writer = new DataWriter(clientSocket.OutputStream);
                Int32 len = (Int32)writer.MeasureString(sendData);

                writer.WriteString(sendData);
                await writer.StoreAsync();

                _status = "Data was sent" + Environment.NewLine;

                writer.DetachStream();
                writer.Dispose();

            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _status = "Send data or receive failed with error: " + exception.Message;

                clientSocket.Dispose();
                clientSocket = null;
                isAuth = false;
                isNews = false;
                connected = false;
            }

            return true;
        }

        public async Task<string> getMSGFromServer()
        {
            _msg = "nothing";

            if (!connected)
            {
                _status = "Must be connected to send!";
                return _msg;
            }

            try
            {
                _status = "Trying to receive data ...";

                DataReader reader = new DataReader(clientSocket.InputStream);
                reader.InputStreamOptions = InputStreamOptions.Partial;

                await reader.LoadAsync(10000000);
                string asdasd = reader.ReadString(reader.UnconsumedBufferLength);

                // переделать всё в два метода по получению новостей отдельно - вход отдельно и тд

                JObject o2 = JObject.Parse(asdasd);


                if (isAuth)
                {
                    if (o2["status"].ToString() == '0'.ToString())
                    {
                        _msg = "bad data";
                    }
                    if (o2["status"].ToString() == '2'.ToString() || o2["status"].ToString() == '1'.ToString())
                    {
                        _msg = "nice";
                    }
                    isAuth = false;
                }

                if (isNews)
                {
                    if (o2["success"].ToString() == '1'.ToString())
                    {
                        _msg = o2["news"].ToString();
                    }
                    else
                    {
                        _msg = "BAD REQUEST";
                    }
                    isNews = false;
                }

                reader.Dispose();
            }
            catch (Exception exception)
            {
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                _status = "Receive failed with error: " + exception.Message;

                clientSocket.Dispose();
                clientSocket = null;
                connected = false;
            }

            return _msg;
        }

        /*private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }*/

        public string getStatus()
        {
            return _status;
        }
    }
}
