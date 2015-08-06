using System.Net;
using System.Net.Sockets;

namespace Network
{
    internal class Socket
    {
        internal string GetHostByName(string addr)
        {
            IPHostEntry hostInfo = Dns.GetHostByName(addr);
            // Get the IP address list that resolves to the host names contained in the
            // Alias property.
            IPAddress[] address = hostInfo.AddressList;
            // Get the alias names of the addresses in the IP address list.
            string[] alias = hostInfo.Aliases;

            return alias[0];
        }

        internal void Listen(int port)
        {
            // do the listen on a port
            if (mSocket == null)
                mSocket = new System.Net.Sockets.Socket(
                    SocketType.Stream, ProtocolType.Tcp);

            mSocket.Listen(port);
        }

        internal Socket Accept()
        {
            return new Socket(mSocket.Accept());
        }

        internal void ConnectTo(string host, int port)
        {
            // connect to a client
            mSocket.Connect(host, port);
        }

        internal int Recv(byte[] buffer)
        {
            return mSocket.Receive(buffer);
        }

        internal int Send(byte[] buffer)
        {
            if (buffer == null)
                return -1;

            return mSocket.Send(buffer);
        }

        Socket(System.Net.Sockets.Socket sock)
        {
            mSocket = sock;
        }

        System.Net.Sockets.Socket mSocket;
    }
}