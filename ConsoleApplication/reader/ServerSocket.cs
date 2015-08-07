using System.Net.Sockets;

namespace Network
{
    internal class ServerSocket
    {

        internal void Listen(int port)
        {
            // do the listen on a port
            if (mSocket == null)
                mSocket = new Socket(
                    SocketType.Stream, ProtocolType.Tcp);

            mSocket.Listen(port);
        }

        internal ServerSocket Accept()
        {
            return new ServerSocket(mSocket.Accept());
        }


        internal int Recv(byte[] buffer)
        {
            return mSocket.Receive(buffer);
        }

        internal ServerSocket()
        {
        }

        ServerSocket(Socket sock)
        {
            mSocket = sock;
        }

        Socket mSocket;
    }
}