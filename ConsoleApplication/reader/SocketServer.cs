using System.Net.Sockets;

namespace Network
{
    internal class SocketServer
    {
        private ClientSocket _clientSocket;

        public SocketServer(ClientSocket clientSocket)
        {
            _clientSocket = clientSocket;
        }

        internal void Listen(int port)
        {
            // do the listen on a port
            if (_clientSocket.mSocket == null)
                _clientSocket.mSocket = new System.Net.Sockets.Socket(
                    SocketType.Stream, ProtocolType.Tcp);

            _clientSocket.mSocket.Listen(port);
        }

        internal ClientSocket Accept()
        {
            return new ClientSocket(_clientSocket.mSocket.Accept());
        }

        internal int Recv(byte[] buffer)
        {
            return _clientSocket.mSocket.Receive(buffer);
        }
    }
}