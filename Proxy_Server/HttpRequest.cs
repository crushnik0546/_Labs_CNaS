using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Proxy_Server
{
    class HttpRequest
    {
        private readonly int DEFAULT_HTTP_PORT = 80;
        public IPHostEntry host;
        public IPAddress ip;
        public int port;
        public string relativeUri;

        public HttpRequest(string request)
        {
            GetHost(request);
        }

        private void GetHost(string request)
        {
            string[] separators = { "\r", "\n"};
            string[] lines = request.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            string preparedHost = "";
            foreach(string line in lines)
            {
                Console.WriteLine($"**{line}**");
                if (line.Contains("Host: "))
                {
                    preparedHost = line.Substring(line.IndexOf("Host: ") + "Host: ".Length);
                    //break;
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
                Console.WriteLine($"ХОСТ КОТОРЫЙ НЕ ИЗВЕСТЕН НАВЕРНОЕ {preparedHost}");

                host = Dns.GetHostEntry(preparedHost);
                ip = host.AddressList[0];
            }
            else
            {
                //exception
            }
        }

    }
}
