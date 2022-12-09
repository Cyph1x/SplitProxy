using ConnectionUtils;
using System;
using System.Net;
using System.Net.Sockets;
public class Server
{
    static int buffSize = 65536;
    ClientConnection[] clients = new ClientConnection[65535];
    byte clientId = 0;
    Socket serverSock = null;

    public void start(int port)
    {
        serverSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        serverSock.Bind(new IPEndPoint(IPAddress.Any, port));
        serverSock.Listen(255);

    }
    public ClientConnection accept()
    {
        while (true)
        {
            Socket clientSock = serverSock.Accept();
            clientSock.ReceiveTimeout = 300000;
            clientSock.SendTimeout = 300000;
            clientSock.ReceiveBufferSize = buffSize;
            clientSock.SendBufferSize = buffSize;
            
            try
            {
                byte[] command = Utils.readExact(clientSock, 2);
                // if the command equals id then we need to send the client an id
                if (command[0] == 'i' && command[1] == 'd')
                {
                    int proxyAmount = BitConverter.ToUInt16(Utils.readExact(clientSock, 2), 0);
                    clients[clientId] = new ClientConnection(proxyAmount);
                    clientSock.Send(BitConverter.GetBytes((UInt16)clientId));
                    clientId++;


                }
                else
                {
                    // the client is requesting a connection
                    // get the connection id
                    int connectionId = BitConverter.ToUInt16(command, 0);
                    int proxyNum = BitConverter.ToUInt16(Utils.readExact(clientSock, 2), 0);
                    clients[connectionId].connections[proxyNum] = new SockConnection(clientSock, proxyNum);
                    clients[connectionId].startReader(proxyNum);
                    // check if all client connections are ready
                    clients[connectionId].connectionAmount++;
                    if (clients[connectionId].connectionAmount == clients[connectionId].connections.Length)
                    {
                        // all connections are ready
                        return clients[connectionId];
                    }
                }
            }catch (SocketException) { clientSock.Close(); }

        }
    }

}

