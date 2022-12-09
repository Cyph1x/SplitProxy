using ConnectionUtils;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;




public class Client
{
    public Socket HttpsProxyConnect(string host, int port, string proxyHost, int proxyPort)
    {
        Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //sock.ReceiveTimeout = 15000;
        //sock.SendTimeout = 15000;
        sock.Connect(proxyHost, proxyPort);
        string request = "CONNECT " + host + ":" + port + " HTTP/1.1\r\n\r\n";
        sock.Send(System.Text.Encoding.ASCII.GetBytes(request));
        byte[] buffer = new byte[1024];
        int read = 0;
        int readamnt;
        // keep reading new data until the received data ends with \r\n\r\n
        while (true)
        {
            readamnt = sock.Receive(buffer, read, 1, SocketFlags.None);
            read += readamnt;
            if (read >= 4 && buffer[read - 4] == '\r' && buffer[read - 3] == '\n' && buffer[read - 2] == '\r' && buffer[read - 1] == '\n')
            {
                break;
            }
            else if (readamnt == 0 || read > buffer.Length)
            {
                return null;
            }
        }
        // check if the server responded with 200 ok
        string response = System.Text.Encoding.Default.GetString(buffer);
        if (response.StartsWith("HTTP/1.1 200") || response.StartsWith("HTTP/1.0 200"))
        {
            return sock;
        }
        else
        {
            Console.WriteLine("Proxy response error: " + response);
            return null;
        }
    }
    Socket targetSock;
    SockConnection[] connections;
    int connectionId;
    String targetHost;
    int targetPort;
    UInt16 readPacketId = 0;
    UInt16 sendPacketId = 0;
    private static EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);

    public Client(String target)
    {
        targetHost = target.Split(':')[0];
        targetPort = int.Parse(target.Split(':')[1]);
    }
    public void connect(String[] proxyList)
    {
        try
        {
            String host = proxyList[0].Split(':')[0];
            int port = int.Parse(proxyList[0].Split(':')[1]);
            connections = new SockConnection[proxyList.Length];
            Socket sock = HttpsProxyConnect(targetHost, targetPort, host, port);
            // send the command "id" and the proxy amount
            byte[] command = new byte[4];
            // set the first 2 bytes to "id"
            byte[] idcommand = new byte[] { 0x69, 0x64 };
            Array.Copy(idcommand, 0, command, 0, 2);
            // set the next 2 bytes to the proxy amount
            Array.Copy(BitConverter.GetBytes((UInt16)proxyList.Length), 0, command, 2, 2);
            sock.Send(command);
            // the server will respond with a UInt16 that is the connection id
            connectionId = BitConverter.ToUInt16(Utils.readExact(sock, 2));
            sock.Close();


            for (int i = 0; i < proxyList.Length; i++)
            {
                host = proxyList[i].Split(':')[0];
                port = int.Parse(proxyList[i].Split(':')[1]);
                Console.WriteLine("creating socket with id" + i.ToString());
                sock = HttpsProxyConnect(targetHost, targetPort, host, port);
                sock.ReceiveTimeout = 300000;
                sock.SendTimeout = 300000;
                // send the connectionid
                Array.Copy(BitConverter.GetBytes((UInt16)connectionId), 0, command, 0, 2);
                // send the proxy number
                Array.Copy(BitConverter.GetBytes((UInt16)i), 0, command, 2, 2);
                sock.Send(command);
                SockConnection connection = new SockConnection(sock, i);

                System.Threading.Thread t = new System.Threading.Thread(() => connectionReader(connection));
                t.Start();
                connection.thread = t;
                connections[i] = connection;

            }
        }
        catch (System.Exception) { }

    }
    public void connectionReader(SockConnection proxySock)
    {
        byte[] info;
        int packetLength;
        int packetId;
        byte[] data;
        try
        {
            while (true)
            {
                // first 2 bytes of info are the packet length
                // next 2 bytes are the packet id

                //Console.WriteLine("thread id: " + Thread.CurrentThread.ManagedThreadId + " Reading packet from socket " + proxySock.id);

                info = Utils.readExact(proxySock, 4);

                //Console.WriteLine("thread id: " + Thread.CurrentThread.ManagedThreadId + " received info: " + BitConverter.ToString(info) + " from socket " + proxySock.id);

                packetLength = BitConverter.ToUInt16(info, 0);
                packetId = BitConverter.ToUInt16(info, 2);
                data = Utils.readExact(proxySock, packetLength);

                //Console.WriteLine("received from server: "+BitConverter.ToString(data).Replace("-", ""));

                while (readPacketId != packetId && proxySock.sock.Connected)
                {
                    ewh.WaitOne();
                }
                ewh.Reset();
                if (!proxySock.sock.Connected)
                {
                    return;
                }

                //Console.WriteLine("sent to client: " + BitConverter.ToString(data).Replace("-", ""));

                // we have the data from the packet
                // send the data to the target socket
                targetSock.Send(data);
                readPacketId++;
                ewh.Set();
            }
        }
        catch (SocketException) { }
        catch (System.ObjectDisposedException) { }

    }
    public void setTarget(Socket sock)
    {
        targetSock = sock;
    }
    public Socket getTarget()
    {
        return targetSock;
    }
    public void write(byte[] data)
    {
        byte[] packet;
        // sort the connections array by the data amount property
        Array.Sort(connections,
delegate (SockConnection x, SockConnection y) { return x.dataAmount.CompareTo(y.dataAmount); });


        // if the length of the data is greater than 65535
        // then split the data into multiple packets
        // otherwise send the data in one packet

        //Console.WriteLine("received from client: " + BitConverter.ToString(data).Replace("-", ""));

        if (data.Length > 65535)
        {
            byte[] packetLength = BitConverter.GetBytes((UInt16)65535);
            byte[] packetId = BitConverter.GetBytes(sendPacketId);
            packet = new byte[65539];
            Array.Copy(packetLength, 0, packet, 0, 2);
            Array.Copy(packetId, 0, packet, 2, 2);
            Array.Copy(data, 0, packet, 4, 65535);
            connections[0].send(packet);
            sendPacketId++;
            write(data.Skip(65535).ToArray());

            //Console.WriteLine("sent to server: " + BitConverter.ToString(data).Replace("-", ""));
        }
        else
        {
            byte[] packetLength = BitConverter.GetBytes((UInt16)data.Length);
            byte[] packetId = BitConverter.GetBytes(sendPacketId);
            packet = new byte[data.Length + 4];
            Array.Copy(packetLength, 0, packet, 0, 2);
            Array.Copy(packetId, 0, packet, 2, 2);
            Array.Copy(data, 0, packet, 4, data.Length);
            connections[0].send(packet);
            sendPacketId++;

            //Console.WriteLine("sent to server: " + BitConverter.ToString(data).Replace("-", ""));
        }

    }

    public void close()
    {
        foreach (SockConnection connection in connections)
        {
            connection.sock.Close();
        }
    }

}
