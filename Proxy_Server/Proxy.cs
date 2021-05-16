using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Proxy_Server
{
    class Proxy
    {
        private readonly int tcpPort = 9998;
        private IPAddress localHost = IPAddress.Parse("127.0.0.1");
        private string errorPagePath = "ErrorPage.html";

        public void Run()
        {
            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(localHost, tcpPort);
                tcpListener.Start();

                while (true)
                { 
                    Socket client = tcpListener.AcceptSocket();
                    Task.Run(() => ClientRequestProcessing(client));
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                tcpListener.Stop();
            }
        }

        private void ClientRequestProcessing(Socket client)
        {
            try
            {
                NetworkStream clientStream = new NetworkStream(client);
                byte[] clientRequest;
                int clientRequestLength;
                (clientRequest, clientRequestLength) = ReadStream(clientStream);

                HttpRequest httpClientRequest = new HttpRequest(Encoding.UTF8.GetString(clientRequest));

                if (httpClientRequest.host != null && IsBlackList(httpClientRequest.host.HostName))
                {
                    ProcessBlackListRequest(clientStream, httpClientRequest);
                    throw new Exception();
                }

                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverHost = new IPEndPoint(httpClientRequest.ip, httpClientRequest.port);

                server.Connect(serverHost);

                NetworkStream serverStream = new NetworkStream(server);
                clientRequest = Encoding.UTF8.GetBytes(httpClientRequest.modifiedRequest);

                serverStream.Write(clientRequest, 0, clientRequest.Length);

                byte[] serverResponse;
                int serverResponseLength;
                (serverResponse, serverResponseLength) = ReadStream(serverStream);
                clientStream.Write(serverResponse, 0, serverResponseLength);

                Log.LogData(httpClientRequest.absolutePath,
                    GetResponse(Encoding.UTF8.GetString(serverResponse), httpClientRequest.host.HostName));
             
                serverStream.CopyTo(clientStream);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
            }
            finally
            {
                client.Close();
            }
        }

        private (byte[], int) ReadStream(NetworkStream stream)
        {
            byte[] data = new byte[20 * 1024];
            byte[] buffer = new byte[1024];
            int i = 0;
            try
            {
                do
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    Array.Copy(buffer, 0, data, i, bytes);
                    i += bytes;
                }
                while (stream.DataAvailable && i < data.Length);

                return (data, i);
            }
            catch (System.IO.IOException)
            {
                // Connection disabled
                throw new SocketException();
            }
        }

        private static string GetResponse(string response, string requestUri)
        {
            string[] separators = { "\r", "\n" };
            string[] lines = response.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            string preparedResponse = "";

            preparedResponse = lines[0].Substring(lines[0].IndexOf(" "));

            if (preparedResponse != "")
            {
                return $"RESPONSE TO {requestUri}\nStatus: " + preparedResponse;
            }
            else
            {
                return null;
            }
        }

        private bool IsBlackList(string hostname)
        {
            bool res = false;

            using (StreamReader reader = new StreamReader("Black_list.txt"))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(hostname))
                    {
                        res = true;
                        break;
                    }
                }
            }
            return res;
        }

        private void ProcessBlackListRequest(NetworkStream clientStream, HttpRequest request)
        {
            byte[] errorResponse = LoadErrorPage();
            clientStream.Write(errorResponse, 0, errorResponse.Length);

            string response = $"RESPONSE TO {request.host.HostName}\nStatus: 403 Forbidden";
            Log.LogData(request.absolutePath, response);
        }

        private byte[] LoadErrorPage()
        {
            using (FileStream fs = new FileStream(errorPagePath, FileMode.Open))
            {
                byte[] page = new byte[fs.Length];
                fs.Read(page, 0, page.Length);

                string header = "HTTP/1.1 403 Forbidden\r\nContent-Type: text/html\r\nContent-Length: "
                            + page.Length + "\r\n\r\n";

                byte[] fullData = new byte[header.Length + page.Length];
                Array.Copy(Encoding.UTF8.GetBytes(header), 0, fullData, 0, Encoding.UTF8.GetBytes(header).Length);
                Array.Copy(page, 0, fullData, Encoding.UTF8.GetBytes(header).Length, page.Length);

                return fullData;
            }
        }
    }
}
