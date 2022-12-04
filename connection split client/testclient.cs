using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
namespace connection_split_client
{
    internal class testclient
    {
        static string[] proxies;
        static string target;
        static void hande(Socket conn)
        {
            Client client = new Client(target);
            try
            {
                
                client.connect(proxies);
                client.setTarget(conn);
                // pipe all the data from conn to the client object
                byte[] data = new byte[65536];
                int len;
                while (true)
                {
                    len = conn.Receive(data);
                    if (len == 0)
                        break;

                    client.write(data.Take(len).ToArray());
                }
            }
            catch (SocketException) { }
            Console.WriteLine("Client disconnected " + conn.RemoteEndPoint);
            client.close();
            conn.Close();
        }
        static void createConfig(int Reason)
        {
            if (Reason == 1)
            {
                Console.WriteLine("Config File is invalid");
                Console.WriteLine("Do you want to create a new config file? (y/n)");
                string answer = Console.ReadLine();
                if (answer.ToLower() == "y")
                {
                    createConfig(0);
                }
                else
                {
                    Console.WriteLine("Please correct the config file and restart the program");
                }
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
                return;
            }
            
            // make a config file
            // create and open a file named config.txt
            Console.WriteLine("Creating config file...");
            StreamWriter file = File.CreateText("config.txt");
            file.WriteLine("#WARNING THIS CONFIG FILE IS STRUCTURED BY LINES");
            file.WriteLine("#DO NOT REMOVE OR MOVE LINES OR FILE WILL BE INVALID");
            file.WriteLine("#You have been warned");
            file.WriteLine("");
            file.WriteLine("#Proxies");
            file.WriteLine("127.0.0.1:8080,127.0.0.1:8080,127.0.0.1:8080,127.0.0.1:8080");
            file.WriteLine("#Target");
            file.WriteLine("127.0.0.1:11111");
            file.Close();
            file.Dispose();
            if (Reason == 0)
            {
                Console.WriteLine("Config Created. Please edit default config and restart the program");

                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
                return;

            }
        }
        static Boolean checkIfValidIp(String IP)
        {
            int port;
            if (!IP.Contains(":")) { return false; }
            String[] split = IP.Split(':');
            if (split.Length != 2) { return false; }
            if (!int.TryParse(split[1],out port)) { return false; }
            if (port < 0 || port > 65535) { return false; }
            IPAddress address;
            if (!IPAddress.TryParse(split[0], out address)) { return false; }
            return true;

        }
        static void loadConfig()
        {
            {
                string[] lines;
                // check if the config file exists
                if (!File.Exists("config.txt"))
                {
                    // the config file hasnt been found
                    createConfig(0);
                }

                // read the config file
                lines = File.ReadAllLines("config.txt");

                // check if the config file is valid
                if (lines[4] != "#Proxies") { createConfig(1); }
                if (lines[6] != "#Target") { createConfig(1); }
                if (!(lines[5].Length > 8)) { createConfig(1); }
                if (!(lines[7].Length > 8)) { createConfig(1); }
                if (!checkIfValidIp(lines[7])) { createConfig(1); }
                String[] ips = lines[5].Split(',');
                foreach (String ip in ips)
                {
                    if (!checkIfValidIp(ip)) { createConfig(1); }
                }
                // config file is valid
                // load the proxies
                lines = File.ReadAllLines("config.txt");
                proxies = lines[5].Split(',');
                target = lines[7];

            }
        }


        static void Main(string[] args)
        {
            loadConfig();
            Socket serverSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serverSock.Bind(new IPEndPoint(IPAddress.Any, 1234));
            serverSock.Listen(255);
            Console.WriteLine("Listening on port 1234");
            while (true)
            {
                Socket sock = serverSock.Accept();
                Console.WriteLine("New connection " + sock.RemoteEndPoint);
                Thread t = new Thread(() => hande(sock));
                t.Start();
            }


        }
    }
}
    
