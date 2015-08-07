using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Network
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerSocket s = new ServerSocket();

            s.Listen(8080);
            s.Accept();

            ClientSocket c = new ClientSocket();

            c.Send(null);

            s.Recv(null);

            DNS.GetHostByName("www.plasticscm.com");

            c.ConnectTo("plasticscm.com", 8008);
        }
    }
}
