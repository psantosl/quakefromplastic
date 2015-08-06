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
            ClientSocket s = new ClientSocket();

            s.SocketServer.Listen(8080);
            s.SocketServer.Accept();

            s.Send(null);

            s.SocketServer.Recv(null);

            s.Dns.GetHostByName("www.plasticscm.com");

            s.ConnectTo("plasticscm.com", 8008);
        }
    }
}
