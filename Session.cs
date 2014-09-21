using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Threading;

namespace ntools.Networking
{
    public abstract class Session
    {
        protected Socket socket;
        private Thread listener;

        internal Session(Socket socket)
        {
            this.socket = socket;

            this.listener = new Thread(Listen);
            this.listener.Start();
        }

        private void Listen()
        {
            byte[] buffer = new byte[32];
            int available = 0;

            while (true)
            {
                if (this.socket.Poll(10000, SelectMode.SelectRead))
                {
                    if (this.socket.Available > 0)
                    {
                        if (this.socket.Available == available)
                        {
                            Read(this.socket.Available);
                            available = 0;
                        }
                        else
                        {
                            available = this.socket.Available;
                            Thread.Sleep(10);
                        }
                    }

                }
                else
                {
                    Thread.Sleep(250);
                }

            }
        }

        private void Read(int size)
        {
            byte[] buffer = new byte[size];
            this.socket.Receive(buffer, size, SocketFlags.None);

            int start = 0;
            int index = 0;
            int index1 = 0;

            while (index < buffer.Length - 1)
            {
                //finding cr
                index = Array.IndexOf(buffer, (byte)13, start);

                //finding lf
                index1 = Array.IndexOf(buffer, (byte)10, index);

                if (index + 1 == index1)
                {
                    //finding space
                    index = Array.IndexOf(buffer, (byte)32);

                    if (index > 0)
                    {
                        OnCommandReceived(new CommandReceivedEventArgs { Socket = this.socket, Command = new string(System.Text.Encoding.UTF8.GetChars(buffer, 0, index)).Trim(), Parameter = new string(System.Text.Encoding.UTF8.GetChars(buffer, index, index1 - index)).Trim() });
                        break;
                    }
                    else
                    {
                        OnCommandReceived(new CommandReceivedEventArgs { Socket = this.socket, Command = new string(System.Text.Encoding.UTF8.GetChars(buffer, 0, index1)).Trim() });
                        break;
                    }
                }
            }
        }

        protected void Send(string cmd, Socket socket = null)
        {
            if (socket == null)
                socket = this.socket;

            socket.Poll(10000, SelectMode.SelectWrite);

            byte[] buffer = new byte[32];
            int read, index = 0;

            while (index < cmd.Length)
            {
                if (cmd.Length - index < 32)
                    read = System.Text.Encoding.UTF8.GetBytes(cmd, index, cmd.Length - index, buffer, 0);
                else
                    read = System.Text.Encoding.UTF8.GetBytes(cmd, index, buffer.Length, buffer, 0);

                socket.Send(buffer, 0, read, SocketFlags.None);
                index += read;
            }

            socket.Send(new byte[] { 0x0D, 0x0A });
        }

        public event CommandReceivedEventHandler CommandReceived;
        public delegate void CommandReceivedEventHandler(object sender, CommandReceivedEventArgs e);
        protected virtual void OnCommandReceived(CommandReceivedEventArgs e)
        {
            if (CommandReceived != null)
                CommandReceived(this, e);
        }
    }

    public class CommandReceivedEventArgs : EventArgs
    {
        public Socket Socket { get; set; }
        public string Command { get; set; }
        public string Parameter { get; set; }

        internal CommandReceivedEventArgs() { }
    }
}
