using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

using ntools.Networking.FTP;

namespace NetduinoFTPServer
{
    public class Program
    {
        public static void Main()
        {
            InputPort exit = new InputPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled);
            ////while (!exit.Read())
            //// write your code here
            FtpServer server = new FtpServer(21);

            while(!exit.Read())
            {
            //    server.Start();

            //    Console.WriteLine("Press any key to stop...");
            //    Console.ReadKey(true);
                Thread.Sleep(20);
            }
        }

    }
}
