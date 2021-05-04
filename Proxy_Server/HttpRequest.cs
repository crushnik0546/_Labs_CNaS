using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;

namespace Proxy_Server
{
    class HttpRequest
    {
        private readonly int DEFAULT_HTTP_PORT = 80;
        public IPHostEntry host;
        public IPAddress ip;
        public int port;
        public string absolutePath;
        public string modifiedRequest;

        public HttpRequest(string request)
        {
            GetHost(request);
            modifiedRequest = ConvertUri(request);
        }

        private void GetHost(string request)
        {
            string[] separators = { "\r", "\n"};
            string[] lines = request.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            string preparedHost = "";
            foreach(string line in lines)
            {
                if (line.Contains("Host: "))
                {
                    preparedHost = line.Substring(line.IndexOf("Host: ") + "Host: ".Length);
                    break;
                }
            }

            if (preparedHost != "")
            {
                if (preparedHost.Contains(":"))
                {
                    port = Int32.Parse(preparedHost.Substring(preparedHost.IndexOf(":") + 1));
                    preparedHost = preparedHost.Substring(0, preparedHost.IndexOf(":"));
                }
                else
                {
                    port = DEFAULT_HTTP_PORT;
                }

                host = Dns.GetHostEntry(preparedHost);
                ip = host.AddressList[0];
            }
            else
            {
                ip = null;
                host = null;
            }
        }

        private string ConvertUri(string request)
        {
            if (request == null) return null;

            const string pattern = @"http:\/\/[a-z0-9а-яё\:\.]*";
            Regex regex = new Regex(pattern);

            MatchCollection matches = regex.Matches(request);
            string uri = matches[0].Value;
            absolutePath = uri;
            string result = request.Replace(uri, "");

            return result;
        }

    }
}
