using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
namespace ConnectionUtils
{
    
    public class ClientConnection
    {
        static int buffSize = 65536;
        Socket targetSock = null;
        public SockConnection[] connections;
        UInt16 readPacketId = 0;
        UInt16 sendPacketId = 0;
        int tempBufferReadHead = 0;
        int tempBufferWriteHead = 0;
        public int connectionAmount = 0;
        byte[] tempBuffer;

        public ClientConnection()
        {

        }
        public ClientConnection(int amnt)
        {
            connections = new SockConnection[amnt];
            tempBuffer = new byte[1024];
        }
        public void startReader(int proxyId)
        {
            System.Threading.Thread t = new System.Threading.Thread(() => connectionReader(connections[proxyId]));
            t.Start();
        }
        public void connectionReader(SockConnection proxySock)
        {
            byte[] info;
            int packetLength;
            int packetId;
            byte[] data;
            try
            {
                while (targetSock==null)
                {
                    // first 2 bytes of info are the packet length
                    // next 2 bytes are the packet id


                    info = Utils.readExact(proxySock, 4);


                    packetLength = BitConverter.ToUInt16(info, 0);
                    packetId = BitConverter.ToUInt16(info, 2);
                    data = Utils.readExact(proxySock, packetLength);


                    // wait for the readPacketId to be updated
                    Monitor.Enter(this);
                    try
                    {
                        while (readPacketId != packetId && proxySock.sock.Connected)
                        {
                            Monitor.Wait(this);
                        }
                        if (!proxySock.sock.Connected)
                        {
                            return;
                        }
                        // we have the data from the packet
                        // if the socket is still set to null then add the data to the temp buffer
                        // otherwise send the data to the target socket
                        if (targetSock == null)
                        {
                            Array.Copy(data, 0, tempBuffer, tempBufferWriteHead, packetLength);
                            tempBufferWriteHead += packetLength;
                        }
                        else
                        {
                            targetSock.Send(data);
                        }
                        Monitor.PulseAll(this);
                    }
                    finally
                    {
                        readPacketId++;
                        Monitor.Exit(this);
                    }

                }

                while (true)
                {
                    // first 2 bytes of info are the packet length
                    // next 2 bytes are the packet id


                    info = Utils.readExact(proxySock, 4);


                    packetLength = BitConverter.ToUInt16(info, 0);
                    packetId = BitConverter.ToUInt16(info, 2);
                    data = Utils.readExact(proxySock, packetLength);


                    // wait for the readPacketId to be updated
                    Monitor.Enter(this);
                    try
                    {
                        while (readPacketId != packetId && proxySock.sock.Connected)
                        {
                            Monitor.Wait(this);
                        }
                        if (!proxySock.sock.Connected)
                        {
                            return;
                        }
                        targetSock.Send(data);
                        Monitor.PulseAll(this);
                    }
                    finally
                    {
                        readPacketId++;
                        Monitor.Exit(this);
                    }

                }
            }
            catch (SocketException)
            {
                //the socket has closed
                // we must now close the whole connection
                close();
            }
            catch (System.ObjectDisposedException)
            { close(); }
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

            connections = connections.OrderBy(c => c.dataAmount).ToArray();

            // if the length of the data is greater than the bufferSize
            // then split the data into multiple packets
            // otherwise send the data in one packet
            if (data.Length > (buffSize - 5))
            {
                packet = new byte[(buffSize - 1)];
                Array.Copy(BitConverter.GetBytes((UInt16)(buffSize - 5)), 0, packet, 0, 2);
                Array.Copy(BitConverter.GetBytes(sendPacketId), 0, packet, 2, 2);
                Array.Copy(data, 0, packet, 4, (buffSize - 5));
                connections[0].send(packet);
                sendPacketId++;
                write(data.Skip((buffSize - 5)).ToArray());
            }
            else
            {
                packet = new byte[data.Length + 4];
                Array.Copy(BitConverter.GetBytes((UInt16)data.Length), 0, packet, 0, 2);
                Array.Copy(BitConverter.GetBytes(sendPacketId), 0, packet, 2, 2);
                Array.Copy(data, 0, packet, 4, data.Length);
                connections[0].send(packet);
                sendPacketId++;

            }


        }

        public byte[] readTempBuffer(int length)
        {
            // get the amount of data from the buffer
            byte[] data = new byte[length];
            while (tempBufferReadHead + length > tempBufferWriteHead)
            {
                Thread.Sleep(1);
            }
            Array.Copy(tempBuffer, tempBufferReadHead, data, 0, length);
            tempBufferReadHead += length;
            return data;
        }
        public void close()
        {
            Console.WriteLine("closing connection");
            foreach (SockConnection connection in connections)
            {
                if (connection != null)
                {
                    connection.sock.Close();
                }
            }
        }
    }
}
