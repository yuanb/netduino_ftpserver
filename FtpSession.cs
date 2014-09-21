using System;
using Microsoft.SPOT;
using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Threading;
namespace ntools.Networking.FTP
{
    public class FtpSession : Session
    {
        private string username, password;
        private bool authorized;
        private string path = "/";
        private string type;
        private IPEndPoint dataEndpoint;
        private string renameFrom;

        internal FtpSession(Socket socket)
            : base(socket)
        {
            this.CommandReceived += OnCommandReceived;

            Send(WELCOME);
        }

        private void OnCommandReceived(object sender, CommandReceivedEventArgs e)
        {
            switch (e.Command)
            {
                case COMMAND_USERNAME:
                    GotUsername(e.Parameter);
                    break;
                case COMMAND_PASSWORD:
                    GotPassword(e.Parameter);
                    break;
                case COMMAND_CURRENTDIRECTORY:
                    PWD();
                    break;
                case COMMAND_SYSTEMTYPE:
                    SYST();
                    break;
                case COMMAND_TYPE:
                    Type(e.Parameter);
                    break;
                case COMMAND_DATAPORT:
                    Port(e.Parameter);
                    break;
                case COMMAND_LIST:
                    List();
                    break;
                case COMMAND_CHANGEDIRECTORY:
                    CWD(e.Parameter);
                    break;
                case COMMAND_CHANGEDIRECTORYUP:
                    CWD("..");
                    break;
                case COMMAND_RETRIEVE:
                    Retrieve(e.Parameter);
                    break;
                case COMMAND_DELETEFILE:
                    Delete(e.Parameter);
                    break;
                case COMMAND_MAKEDIR:
                    MKD(e.Parameter);
                    break;
                case COMMAND_REMOVEDIR:
                    RemoveDirectory(e.Parameter);
                    break;
                case COMMAND_STORE:
                    Store(e.Parameter);
                    break;
                case COMMAND_RENAMEFROM:
                    RenameFrom(e.Parameter);
                    break;
                case COMMAND_RENAMETO:
                    RenameTo(e.Parameter);
                    break;

                default:
                    NotImplemented();
                    break;
            }
        }

        private void GotUsername(string u)
        {
            if (this.authorized || this.username != null)
                ForceClose();
            else
            {
                this.username = u;
                Send(STATUS_USERNAME);
            }
        }

        private void GotPassword(string p)
        {
            if (this.authorized)
                ForceClose();
            else
            {
                this.password = p;
                this.authorized = true;
                Send(STATUS_PASSWORD);
            }
        }

        private void PWD()
        {
            if (this.authorized)
                Send(STATUS_PWD + "\"" + path + "\" is current directory");
            else
                ForceClose();
        }

        private void SYST()
        {
            if (this.authorized)
                Send("AnySense Type: A");
            else
                ForceClose();
        }

        private void Type(string type)
        {
            this.type = type;
            Send(STATUS_OK);
        }

        private void Port(string p)
        {
            string[] arr = p.Split(',');
            byte[] ip = new byte[4];

            for (int i = 0; i < 4; i++)
                ip[i] = (byte)Int16.Parse(arr[i]);

            int port = Int16.Parse(arr[4]) * 256 + Int16.Parse(arr[5]);

            this.dataEndpoint = new IPEndPoint(new IPAddress(ip), port);

            Send(STATUS_OK);
        }

        private void List()
        {
            if (this.authorized)
            {
                Send(STATUS_OPENCONNECTION);

                var enumerator = Directory.EnumerateDirectories(ConvertPath(this.path)).GetEnumerator();

                using (Socket datasocket = OpenDataConnection())
                {
                    using (NetworkStream ns = new NetworkStream(datasocket))
                    {
                        using (StreamWriter sw = new StreamWriter(ns))
                        {
                            while (enumerator.MoveNext())
                            {
                                DirectoryInfo d = new DirectoryInfo((string)enumerator.Current);
                                string date = d.LastWriteTime.ToString("MMM dd HH:mm");
                                string line = "drwxr-xr-x    2 2003     2003     4096     " + date + " " + d.Name;
                                sw.WriteLine(line);
                                sw.Flush();
                            }

                            enumerator = Directory.EnumerateFiles(ConvertPath(this.path)).GetEnumerator();

                            while (enumerator.MoveNext())
                            {
                                FileInfo f = new FileInfo((string)enumerator.Current);
                                string date = f.LastWriteTime.ToString("MMM dd HH:mm");
                                string line = "-rw-r--r--    2 2003     2003     " + f.Length + " " + date + "  " + f.Name;
                                sw.WriteLine(line);
                                sw.Flush();
                            }
                        }
                    }
                }

                Send(STATUS_TRANSFERCOMPLETE);
            }
            else
            {
                ForceClose();
            }
        }

        private void CWD(string path)
        {
            if (this.authorized)
            {
                if (path == "..")
                    this.path = this.path.Substring(0, this.path.LastIndexOf('/'));
                else if (path.ToCharArray()[0] == '/')
                    this.path = path;
                else
                    this.path += '/' + path;

                //if (path == "..")
                //{
                //    if (this.path.IndexOf('/') == -1)
                //        this.path = "/";
                //    else
                //        this.path = this.path.Substring(0, this.path.LastIndexOf('/') + 1);
                //}
                //else
                //    this.path = path;
                //{
                //    if (this.path.ToCharArray()[this.path.Length - 1] == '/')
                //        this.path += path;
                //    else
                //        this.path += '/' + path;
                //}

                Send(STATUS_OK);
            }
            else
                ForceClose();
        }

        private void Retrieve(string file)
        {
            file = ConvertPath(this.path) + "\\" + file;

            if (this.authorized && File.Exists(file))
            {
                Send(STATUS_OPENCONNECTION);

                using (Socket dataSocket = OpenDataConnection())
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open))
                    {
                        using (NetworkStream ns = new NetworkStream(dataSocket))
                        {
                            byte[] buffer = new byte[64];
                            int len;

                            do
                            {
                                len = fs.Read(buffer, 0, buffer.Length);
                                ns.Write(buffer, 0, len);
                            }
                            while (len == buffer.Length);
                        }
                    }
                }

                Send(STATUS_TRANSFERCOMPLETE);
            }
            else
            {
                ForceClose();
            }
        }

        private void Delete(string file)
        {
            file = ConvertPath(this.path) + "\\" + file;

            if (this.authorized && File.Exists(file))
            {
                File.Delete(file);
                Send(STATUS_OK);
            }
            else
            {
                ForceClose();
            }

        }

        private void RemoveDirectory(string path)
        {
            path = ConvertPath(this.path) + "\\" + path;

            if (this.authorized && Directory.Exists(path))
            {
                Directory.Delete(path);
                Send(STATUS_OK);
            }
            else
            {
                ForceClose();
            }
        }

        private void MKD(string folder)
        {
            if (this.authorized)
            {
                folder = ConvertPath(this.path) + "\\" + folder;
                Directory.CreateDirectory(folder);

                Send(STATUS_OK);
            }
            else
            {
                ForceClose();
            }

        }

        private void RenameFrom(string from)
        {
            if (this.authorized)
            {
                this.renameFrom = from;
                Send(STATUS_OK);
            }
            else
                ForceClose();
        }

        private void RenameTo(string to)
        {
            if (this.authorized)
            {
                if (this.renameFrom != null)
                {
                    string from = ConvertPath(this.path) + "\\" + this.renameFrom;
                    to = ConvertPath(this.path) + "\\" + to;

                    if (File.GetAttributes(from) == FileAttributes.Directory)
                        Directory.Move(from, to);
                    else
                        File.Move(from, to);

                    Send(STATUS_OK);
                }
                else
                {
                    Send(STATUS_FILEUNAVAILABLE);
                }
            }
            else
            {
                ForceClose();
            }
        }

        private void ForceClose()
        {
        }

        private Socket OpenDataConnection()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(this.dataEndpoint);
            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Tcp, System.Net.Sockets.SocketOptionName.NoDelay, true);
            socket.SendTimeout = 5000;
            return socket;
        }

        private void Store(string file)
        {
            if (this.authorized)
            {
                file = ConvertPath(this.path) + "\\" + file;

                Send(STATUS_OPENCONNECTION);

                using (FileStream fs = new FileStream(file, FileMode.OpenOrCreate))
                {
                    using (Socket socket = OpenDataConnection())
                    {
                        using (NetworkStream ns = new NetworkStream(socket))
                        {
                            byte[] buffer = new byte[1024];
                            int len=0;
                            do
                            {
                                len = ns.Read(buffer, 0, buffer.Length);
                                fs.Write(buffer, 0, len);
                            } while (len>0);
                            ns.Close();
                            ns.Dispose();
                        }
                    }
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();
                }

                Send(STATUS_TRANSFERCOMPLETE);
            }
            else
            {
                ForceClose();
            }
        }

        private void NotImplemented()
        {
            Send(STATUS_NOTIMPLEMENTED);
        }

        private string ConvertPath(string path)
        {
            if (path == "/")
                return "\\SD\\";
            else
                return "\\SD\\" + path.Replace("/", "\\");
        }

        private const string WELCOME = "220 Welcome to Netduino FTP server.";
        private const string STATUS_USERNAME = "331 User name ok. Enter password.";
        private const string STATUS_PASSWORD = "230 User logged in.";
        private const string STATUS_PWD = "257 ";
        private const string STATUS_NOTIMPLEMENTED = "502 Command not implemented.";
        private const string STATUS_OK = "200 OK";
        private const string STATUS_OPENCONNECTION = "150 Status okay, opening data connection.";
        private const string STATUS_TRANSFERCOMPLETE = "223 Transfer complete.";
        private const string STATUS_FILEUNAVAILABLE = "550 File unavailable";

        private const string COMMAND_USERNAME = "USER";
        private const string COMMAND_PASSWORD = "PASS";
        private const string COMMAND_CURRENTDIRECTORY = "PWD";
        private const string COMMAND_SYSTEMTYPE = "SYST";
        private const string COMMAND_TYPE = "TYPE";
        private const string COMMAND_DATAPORT = "PORT";
        private const string COMMAND_LIST = "LIST";
        private const string COMMAND_CHANGEDIRECTORY = "CWD";
        private const string COMMAND_CHANGEDIRECTORYUP = "CDUP";
        private const string COMMAND_RETRIEVE = "RETR";
        private const string COMMAND_STORE = "STOR";
        private const string COMMAND_DELETEFILE = "DELE";
        private const string COMMAND_MAKEDIR = "MKD";
        private const string COMMAND_REMOVEDIR = "RMD";
        private const string COMMAND_RENAMEFROM = "RNFR";
        private const string COMMAND_RENAMETO = "RNTO";
    }
}
