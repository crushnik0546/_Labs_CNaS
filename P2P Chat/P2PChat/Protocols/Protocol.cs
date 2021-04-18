using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using P2PChat.Clients_Mess;

namespace P2PChat.Protocols
{
    class Protocol
    {
        public delegate void UpdateWindowChat(string text);

        private const int udpPort = 9500;
        private const int tcpPort = 10000;
        private string myOwnLogin;
        private List<Client> clients = new List<Client>();
        public IPAddress chooseIP;
        public UpdateWindowChat updateChat;
        private StringBuilder chatHistory;
        private DateTime currentTime;
        private readonly SynchronizationContext synchronizationContext;

        public Protocol(UpdateWindowChat del)
        {
            updateChat = del;
            chatHistory = new StringBuilder();
            currentTime = new DateTime();
            synchronizationContext = SynchronizationContext.Current;
        }

        public void ConnectionToChat(string login)
        {
            IPEndPoint srcIP = new IPEndPoint(chooseIP, udpPort);
            IPEndPoint destIP = new IPEndPoint(MakeBroadcastAdr(chooseIP), udpPort);
            UdpClient udpClient = new UdpClient(srcIP);
            udpClient.EnableBroadcast = true;

            myOwnLogin = login;
            byte[] connectMessBytes = Encoding.UTF8.GetBytes(login);

            try
            {
                udpClient.Send(connectMessBytes, connectMessBytes.Length, destIP);
                udpClient.Close();

                currentTime = DateTime.Now;
                string connectMess = $"{currentTime} : IP [{chooseIP}] : {login} подключился к чату\n";
                chatHistory.Append(connectMess);
                updateChat($"{currentTime} : IP [{chooseIP}] Вы ({login}) подключились к чату\n");

                Task recieveUdpBroadcast = new Task(ReceiveBroadcast);
                recieveUdpBroadcast.Start();

                Task recieveTCP = new Task(ReceiveTCP);
                recieveTCP.Start();
            }
           catch
           {
               MessageBox.Show("Sending Error!", "BAD", MessageBoxButton.OKCancel);
           }
        }

        private void ReceiveBroadcast()
        {
            IPEndPoint srcIP = new IPEndPoint(chooseIP, udpPort);
            IPEndPoint destIP = new IPEndPoint(IPAddress.Any, udpPort);
            UdpClient udpReceiver = new UdpClient(srcIP);
            //isReceiveUdp = true;

            while(true)
            {
                byte[] receivedData = udpReceiver.Receive(ref destIP);
                string clientLogin = Encoding.UTF8.GetString(receivedData);

                Client newClient = new Client(clientLogin, destIP.Address, tcpPort);
                newClient.EstablishConnection();
                clients.Add(newClient);
                newClient.SendMessage(new Message(Message.CONNECT, myOwnLogin));

                currentTime = DateTime.Now;
                string infoMess = $"{currentTime} : IP [{newClient.IP}] {newClient.login} : Присоединился к чату\n";

                synchronizationContext.Post(delegate { updateChat(infoMess); }, null);
                

                Task.Factory.StartNew(() => ListenClient(newClient));
            }

            //udpReceiver.Close();
        }

        private void ReceiveTCP()
        {
            TcpListener tcpListener = new TcpListener(chooseIP, tcpPort);
            tcpListener.Start();

            while(true)
            {
                TcpClient tcpNewClient = tcpListener.AcceptTcpClient();
                Client newClient = new Client(tcpNewClient, tcpPort);

                Task.Factory.StartNew(() => ListenClient(newClient));
            }

        }

        private void ListenClient(Client client)
        {
            while (true)
            {
                if (client.messStream.DataAvailable)
                {
                    Message tcpMessage = client.ReceiveMessage();
                    string infoMes;

                    switch (tcpMessage.code)
                    {
                        case Message.CONNECT:
                            client.login = tcpMessage.data;
                            clients.Add(client);
                            GetHistoryMessageToConnect(client);
                            break;

                        case Message.MESSAGE:
                            currentTime = DateTime.Now;
                            infoMes = $"{currentTime} : IP [{client.IP}] {client.login} : {tcpMessage.data}\n";
                            synchronizationContext.Post(delegate { updateChat(infoMes); chatHistory.Append(infoMes); }, null);
                            break;

                        case Message.DISCONNECT:
                            currentTime = DateTime.Now;
                            infoMes = $"{currentTime} : IP [{client.IP}] {client.login} Покинул чат\n";
                            synchronizationContext.Post(delegate { updateChat(infoMes); chatHistory.Append(infoMes); }, null);
                            clients.Remove(client);
                            break;

                        case Message.GET_HISTORY:
                            SendHistoryMessage(client);
                            break;

                        case Message.SHOW_HISTORY:
                            synchronizationContext.Post(delegate { updateChat(tcpMessage.data); chatHistory.Append(tcpMessage.data); }, null);
                            break;

                        default:
                            MessageBox.Show("Неверный формат сообщения", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                }
            }
        }

        public void SendHistoryMessage(Client client)
        {
            Message historyMessage = new Message(Message.SHOW_HISTORY, chatHistory.ToString());
            client.SendMessage(historyMessage);
        }

        public void GetHistoryMessageToConnect(Client client)
        {
            Message historyMessage = new Message(Message.GET_HISTORY, "");
            client.SendMessage(historyMessage);
        }

        public void SendDisconnectMessage()
        {
            string disconnectStr = $"{myOwnLogin} покинул чат";
            Message disconnectMes = new Message(Message.DISCONNECT, disconnectStr);
            SendMessageToAllClients(disconnectMes);
        }

        public void SendNormalMessage(string mes)
        {
            if (mes != "")
            {
                Message normalMess = new Message(Message.MESSAGE, mes);
                SendMessageToAllClients(normalMess);
            }
        }

        public void SendMessageToAllClients(Message tcpMes)
        {
            foreach (var user in clients)
            {
                try
                {
                        user.SendMessage(tcpMes);
                }
                catch
                {
                    MessageBox.Show($"Не удалось отправить сообщение пользователю {user.login}.",
                        "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (tcpMes.code == Message.MESSAGE)
            {
                currentTime = DateTime.Now;
                string infoMessage = $"{currentTime} : IP [{chooseIP}] {myOwnLogin} : {tcpMes.data}\n";

                updateChat(infoMessage);
                chatHistory.Append(infoMessage);
            }

        }

        private IPAddress MakeBroadcastAdr(IPAddress ip)
        {
            string broadcast_adr = ip.ToString();
            broadcast_adr = broadcast_adr.Substring(0, broadcast_adr.LastIndexOf('.') + 1) + "255";

            return IPAddress.Parse(broadcast_adr);
        }

    }
}
