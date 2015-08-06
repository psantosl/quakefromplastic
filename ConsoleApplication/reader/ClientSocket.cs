using System.Net;
using System.Net.Sockets;

namespace Network
{
    internal class ClientSocket
    {
        internal void ConnectTo(string host, int port)
        {
            // connect to a client
            mSocket.Connect(host, port);
        }

        internal int Send(byte[] buffer)
        {
            if (buffer == null)
                return -1;

            return mSocket.Send(buffer);
        }

        ClientSocket(System.Net.Sockets.Socket sock)
        {
            mSocket = sock;
        }

        public ClientSocket()
        {
        }

        Socket mSocket;
    }
}