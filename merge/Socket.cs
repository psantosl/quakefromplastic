using System.Net;

namespace Network
{
    internal class Socket
    {
        internal string GetHostByName(string addr)
        {
            // this method returns the host
            // when you give an IP
            return CalculateHostByName(addr);
        }

        internal void Listen()
        {
            // do the listen on a port
            // and whatever it is needed
            // to listen
        }

        internal void ConnectTo(string addr)
        {
            // connect to a client
            Net.ConnectTo(addr);
        }

        internal int Send(byte[] buffer)
        {
            System.IO.Write(buffer);
        }

        internal int Recv(byte[] buffer)
        {
            System.IO.Read(buffer);
        }
    }
}
