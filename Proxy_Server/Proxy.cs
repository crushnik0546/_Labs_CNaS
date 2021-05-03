using System;
using System.Collections.Generic;
using System.Linq;
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
                    //Task.Factory.StartNew(() => ClientRequestProcessing(client));
                    Task.Run(() => ClientRequestProcessing(client));
                    // Shutdown and end connection
                    //client.Close();
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
                //string clientRequest = ReadStream(clientStream);
                byte[] clientRequest;
                int clientRequestLength;
                (clientRequest, clientRequestLength) = ReadStream(clientStream);

                if (Encoding.UTF8.GetString(clientRequest).Contains("CONNECT"))
                    return;

                //Console.WriteLine("ЧТО-НИБУДЬ!");

                //Console.WriteLine($"REQUEST\n{clientRequest}");

                HttpRequest httpClientRequest = new HttpRequest(Encoding.UTF8.GetString(clientRequest));

                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverHost = new IPEndPoint(httpClientRequest.ip, httpClientRequest.port);

                server.Connect(serverHost);

                NetworkStream serverStream = new NetworkStream(server);
                clientRequest = Encoding.UTF8.GetBytes(ConvertPath(Encoding.UTF8.GetString(clientRequest)));

                Console.WriteLine($"REQUEST\n{Encoding.UTF8.GetString(clientRequest).Split('\0')[0]}");

                //byte[] clientRequestBytes = Encoding.UTF8.GetBytes(clientRequest);
                serverStream.Write(clientRequest, 0, clientRequest.Length);

                //string serverResponse = ReadStream(serverStream);
                //byte[] serverResponseBytes = Encoding.UTF8.GetBytes(serverResponse);
                byte[] serverResponse;
                int serverResponseLength;
                (serverResponse, serverResponseLength) = ReadStream(serverStream);
                clientStream.Write(serverResponse, 0, serverResponseLength);

                Console.WriteLine($"RESPONSE\n{ExtractResponseCode(Encoding.UTF8.GetString(serverResponse))}");
                serverStream.CopyTo(clientStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                client.Close();
            }
            //Console.WriteLine("КОНЕЦ ЧТО-НИБУДЬ!");
        }

        private (byte[], int) ReadStream(NetworkStream stream)
        {
            //StringBuilder messageData = new StringBuilder();
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
                    //messageData.Append(Encoding.UTF8.GetString(data, 0, bytes));
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

        private string ConvertPath(string input)
        {
            if (input == null) return null;

            const string pattern = @"http:\/\/[a-z0-9а-яё\:\.]*";
            var regex = new Regex(pattern);

            var matches = regex.Matches(input);
            string host = matches[0].Value;
            string result = input.Replace(host, "");

            return result;
        }

        private static string ExtractResponseCode(string data)
        {
            string[] dataArray = data.Split('\r', '\n');
            int indexCode = dataArray[0].IndexOf(" ", StringComparison.Ordinal) + 1;
            string code = dataArray[0].Substring(indexCode);

            return code;
        }
    }
}
