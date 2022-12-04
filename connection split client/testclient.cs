using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ConnectionUtils;
namespace connection_split_client
{
    internal class testclient
    {
        static void hande(Socket conn)
        {
            Client client = new Client("127.0.0.1:11111");
            try
            {
                
                // 8 proxy connections
                String[] proxies = { "127.0.0.1:8080", "127.0.0.1:8080", "127.0.0.1:8080", "127.0.0.1:8080", "127.0.0.1:8080", "127.0.0.1:8080", "127.0.0.1:8080", "127.0.0.1:8080" };
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
            }catch (SocketException){ }
            Console.WriteLine("Client disconnected " + conn.RemoteEndPoint);
            client.close();
            conn.Close();
        }
        static void Main(string[] args)
        {
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
