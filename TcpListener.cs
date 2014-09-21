using System;
using Microsoft.SPOT;
using System.Threading;
using System.Net.Sockets;
using System.Net;


namespace ntools.Networking
{
    public class TcpListener
    {
        private Thread listener;
        private Socket socket;
        private bool listen = true;

        public TcpListener(int port)
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Bind(new IPEndPoint(IPAddress.Any, port));
            this.socket.Listen(4);
            this.listener = new Thread(Listen);
            this.listener.Start();
        }

        public void Listen()
        {
            while (this.listen)
            {
                if (this.socket.Poll(10000, SelectMode.SelectRead))
                {
                    OnClientConnected(new ClientConnectedEventArgs { Socket = this.socket.Accept() });
                }
                Thread.Sleep(500);
            }
        }

        public event ClientConnectedEventHandler ClientConnected;
        public delegate void ClientConnectedEventHandler(object sender, ClientConnectedEventArgs e);
        protected virtual void OnClientConnected(ClientConnectedEventArgs e)
        {
            if (ClientConnected != null)
                ClientConnected(this, e);
        }


    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public Socket Socket { get; set; }

        internal ClientConnectedEventArgs()
        { }
    }
}
