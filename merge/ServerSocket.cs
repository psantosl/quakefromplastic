using System.Net;

namespace Network
{
    internal class ServerSocket
    {
        internal int Recv(byte[] buffer)
        {
            System.IO.Read(buffer);
        }

        internal void Listen()
        {
            // do the listen on a port
            // and whatever it is needed
            // to listen
            mSocket.Recv();
        }
    }
}
