using System.Net;
using System.Net.Sockets;

namespace Network
{
    internal class ClientSocket
    {
        internal void ConnectTo(string host, int port)
        {
            mSocket = new Socket(
                SocketType.Stream, ProtocolType.Tcp);

            // connect to a client
            mSocket.Connect(host, port);
        }

        internal int Send(byte[] buffer)
        {
            if (buffer == null)
                return -1;

            return mSocket.Send(buffer);
        }

        Socket mSocket;
    }
}