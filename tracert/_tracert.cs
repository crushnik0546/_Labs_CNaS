using System;
using System.Net;
using System.Net.Sockets;

namespace tracert
{
    class _tracert
    {
        static void Main(string[] args)
        {
            bool show_hostname = false;
            string adress;

            if (args.Length == 0)
            {
                Console.WriteLine("Не удается разрешить системное имя узла \"\" ");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            } 
            
            if (args[0] == "-n")
            {
                show_hostname = true;
                adress = args[1];
            }
            else
            {
                adress = args[0];
            }

            ICMP packet = new ICMP();
            packet.data_size = packet.data.Length;
            packet.CalcCheckSum();

            int packet_size = packet.data_size + 8;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

             try
            {
                IPHostEntry ipHost = Dns.GetHostEntry(adress);
                IPEndPoint ipEndPoint = new IPEndPoint(ipHost.AddressList[0], 0);
                EndPoint endpoint = ipEndPoint;

                Console.WriteLine("\nТрассировка маршрута к " + adress);
                Console.WriteLine("с максимальным количеством прыжков 30:\n");

                for (int i = 1; i <= 30; i++)
                {
                    int error_count = 0;
                    Console.Write("{0, 2}", i);
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);

                    bool stop = false;

                    for (int j = 1; j <= 3; j++)
                    {
                        byte[] buffer = new byte[2048];
                        DateTime start_time = DateTime.Now;

                        try
                        {
                            socket.SendTo(packet.GetBytes(), packet_size, SocketFlags.None, ipEndPoint);
                            int recived_bytes = socket.ReceiveFrom(buffer, ref endpoint);

                            TimeSpan delta = DateTime.Now - start_time;
                            ICMP reply = new ICMP(buffer, recived_bytes);

                            if (reply.type == 11)
                            {
                                Console.Write("{0, 10}", delta.Milliseconds + " мс");
                            }

                            if (reply.type == 0)
                            {
                                Console.Write("{0, 10}", delta.Milliseconds + " мс");
                                stop = true;
                            }
                        }
                        catch (SocketException)
                        {
                            Console.Write("{0, 10}", "*");
                            error_count++;
                        }
                    }

                    if (show_hostname)
                    {
                        try
                        {
                            string name = Dns.GetHostEntry(IPAddress.Parse(endpoint.ToString().Split(':')[0])).HostName;
                            Console.Write($"  {name} [{endpoint.ToString().Split(':')[0]}]\n");
                        }
                        catch (SocketException)
                        {
                            Console.Write($"  {endpoint.ToString().Split(':')[0]}\n");
                        }
                    }
                    else
                    {
                        Console.Write($"  {endpoint.ToString().Split(':')[0]}\n");
                    }
                    

                    if (stop)
                    {
                        Console.Write("\nТрассировка завершена.");
                        break;
                    }

                    if (error_count == 3)
                        Console.Write("  Превышен интервал ожидания для запроса.\n");

                }
            }
            catch
            {
                Console.WriteLine("Не удается разрешить системное имя узла {0}", adress);
            }
            
            socket.Close();
        }
    }

    public class ICMP
    {
        public byte type;
        public byte code;
        private ushort check_sum;
        public ushort id;
        public ushort number;
        public int data_size;
        public byte[] data = new byte[1024];


        public ICMP()
        {
            type = 8;
        }

        public ICMP(byte[] packet, int count_of_bytes)
        {
            type = packet[20];
            code = packet[21];
            check_sum = BitConverter.ToUInt16(packet, 22);
            id = BitConverter.ToUInt16(packet, 24);
            number = BitConverter.ToUInt16(packet, 26);
            data_size = count_of_bytes - 28;
            Buffer.BlockCopy(packet, 28, data, 0, data_size);
        }

        public byte[] GetBytes()
        {
            byte[] byte_data = new byte[data_size + 8];
            Buffer.BlockCopy(BitConverter.GetBytes(type), 0, byte_data, 0, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(code), 0, byte_data, 1, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(check_sum), 0, byte_data, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(id), 0, byte_data, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(number), 0, byte_data, 6, 2);
            Buffer.BlockCopy(data, 0, byte_data, 8, data_size);
            return byte_data;
        }

        public void CalcCheckSum()
        {
            long sum = 0;
            byte[] byte_data = GetBytes();
            int index = 0, count = data_size;

            while (count > 1)
            {
                sum += Convert.ToUInt32(BitConverter.ToUInt16(byte_data, index));
                count -= 2;
                index += 2;
            }
            if (count > 0)
                sum += Convert.ToUInt32(BitConverter.ToUInt16(byte_data, index));

            while (sum >> 16 != 0)
                sum = (sum & 0xffff) + (sum >> 16);

            check_sum = (ushort)(~sum);
        }


    }
}