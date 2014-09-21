using System;
using Microsoft.SPOT;
using System.Collections;

namespace ntools.Networking.FTP
{
    public class FtpServer
    {
        private TcpListener listener;
        private ArrayList sessions;

        public FtpServer(int port)
        {
            this.sessions = new ArrayList();
            this.listener = new TcpListener(port);
            this.listener.ClientConnected += OnClientConnected;
        }

        private void OnClientConnected(object sender, ClientConnectedEventArgs e)
        {
            this.sessions.Add(new FtpSession(e.Socket));
        }

    }
}
