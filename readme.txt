This is a slightly modified version of FTP server found from http://ntools.codeplex.com/

a. NetworkStream buffer size is increased from 64 to 1024.
b. FtpSession::Store function is modified to following

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

Tested on netduino plus 2. Working with FileZilla, STOR speed is about 20.9kB/s
