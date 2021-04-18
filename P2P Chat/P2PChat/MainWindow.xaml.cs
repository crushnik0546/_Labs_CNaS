using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using P2PChat.Additional;
using System.Net;
using P2PChat.Protocols;

namespace P2PChat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<IPAddress> ipList;
        private IPAddress selectedIP;
        private Protocol connection;

        public MainWindow()
        {
            InitializeComponent();

            txtbxMessage.IsEnabled = false;
            btnSend.IsEnabled = false;
            txtboxChatWindow.IsReadOnly = true;
            btnConnect.IsDefault = true;
            txtboxChatWindow.ScrollToEnd();

            ipList = Funcs.GetIPList();
            foreach(IPAddress ip in ipList)
            {
                cmboxUserIP.Items.Add(ip.ToString());
            }
            cmboxUserIP.SelectedIndex = 0;
            selectedIP = ipList[0];


            connection = new Protocol(UpdateChat);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            connection.chooseIP = selectedIP;
            connection.ConnectionToChat(txtbxLogin.Text);
            btnConnect.IsEnabled = false;
            cmboxUserIP.IsEnabled = false;
            btnConnect.IsDefault = false;
            txtbxLogin.IsReadOnly = true;

            btnSend.IsDefault = true;
            txtbxMessage.IsEnabled = true;
            btnSend.IsEnabled = true;
        }

        private void cmboxUserIP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIP = IPAddress.Parse(cmboxUserIP.SelectedItem.ToString());
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string currMess = txtbxMessage.Text;
            connection.SendNormalMessage(currMess);
            txtbxMessage.Text = "";
        }

        private void UpdateChat(string text)
        {
            txtboxChatWindow.AppendText(text);
        }

        private void formMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connection.SendDisconnectMessage();
            System.Environment.Exit(0);
        }
    }
}
