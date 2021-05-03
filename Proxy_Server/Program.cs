using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Proxy pr = new Proxy();
            pr.Run();

        }
    }
}
