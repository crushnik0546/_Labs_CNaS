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

                if (Encoding.UTF8.GetString(clientRequest).Contains("CONNECT"))
                    return;

                HttpRequest httpClientRequest = new HttpRequest(Encoding.UTF8.GetString(clientRequest));

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

                Log.LogData(Encoding.UTF8.GetString(clientRequest).Split('\0')[0],
                    GetResponse(Encoding.UTF8.GetString(serverResponse), httpClientRequest.absolutePath));
             
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
    }
}
