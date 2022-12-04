using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ConnectionUtils;

namespace connection_split_server
{
    internal class testserver
    {
        static Boolean compareByteArray(byte[] arr1, byte[] arr2)
        {
            // first check that both arrays are the same length
            if (arr1.Length != arr2.Length) { return false; }
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i]) { return false; }
            }
            return true;
        }
        static Boolean compareByteArray(byte[] arr1, int[] arr2)
        {
            // first check that both arrays are the same length
            if (arr1.Length != arr2.Length) { return false; }
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i]) { return false; }
            }
            return true;
        }
        static void handle(ClientConnection client)
        {
            // SOCKS5
            // https://tools.ietf.org/html/rfc1928
            // check if the ver is 5 and if the number of methods is 1 and if the method is 0
            byte[] tempBuffer;
            tempBuffer = client.readTempBuffer(3);
            if (!compareByteArray(tempBuffer,new int[] {5,1,0}))
            {
                // close
                return;
            }
            // tell the client that no auth is required
            client.write(new byte[] { 0x05, 0x00 });
            byte[] command = client.readTempBuffer(4);
            if (command[1] != 1)
            {
                //close
                return;
            }
            int connectionType = command[3];
            String dest = "";
            int port;
            if (connectionType == 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    dest += client.readTempBuffer(1).ToString();
                    if (i != 3)
                    {
                        dest += ".";
                    }

                }
                port = BitConverter.ToUInt16(client.readTempBuffer(2).Reverse().ToArray(), 0);
            }
            else if (connectionType==3)
            {
                int domainLength = client.readTempBuffer(1)[0];
                dest += System.Text.Encoding.Default.GetString(client.readTempBuffer(domainLength));
                port = BitConverter.ToUInt16(client.readTempBuffer(2).Reverse().ToArray(), 0);
            }
            else
            {
                client.write(new byte[] { 0x05, 0x08, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                // close
                return;
            }
            Console.WriteLine(dest);
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //PUT TRY CATCH AROUND THIS!!!!!
            Console.WriteLine(dest + ":" + port.ToString());
                sock.Connect(dest, port);
            client.setTarget(sock);
            client.write(new byte[] { 0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            // pipe all the data from conn to the client object
            byte[] data = new byte[65536];
            int len;
            try
            {
                while (true)
                {
                    len = sock.Receive(data);
                    if (len == 0)
                        break;
                    client.write(data.Take(len).ToArray());
                }
            }catch (System.ObjectDisposedException) { }// connection closed

        }
        static void Main(string[] args)
        {
            Server server = new Server();
            server.start(11111);
            Console.WriteLine("Listening on port 11111");
            while (true)
            {
                ClientConnection client = server.accept();
                Console.WriteLine("New connection!!!!!");
                System.Threading.Thread t = new System.Threading.Thread(() => handle(client));
                t.Start();
            }
            
        }
    }
}
