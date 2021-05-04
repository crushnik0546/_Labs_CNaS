using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Proxy_Server
{
    public static class Log
    {
        static object locker = new object();

        public static void LogData(string request, string response)
        {
            lock(locker)
            {
                using (StreamWriter writer = File.AppendText("History.log"))
                {
                    writer.WriteLine("REQUEST\n" + request);
                    writer.WriteLine(response + "\n");
                }

                Console.WriteLine($"REQUEST\n{request}");
                Console.WriteLine($"{response}\n");
            }
        }

    }
}
