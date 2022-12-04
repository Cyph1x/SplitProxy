using System;
using System.Net;
using System.Net.Sockets;
namespace proxy_server
{
    internal class Program
    {

        static int threadAmnt = 0;

        static void tryClose(Socket sock)
        {
            try
            {
                sock.Shutdown(SocketShutdown.Both);

            }
            catch (Exception)
            {
            }
            try
            {
                sock.Close();

            }
            catch (Exception)
            {
            }
        }
        static void pipe(Socket sock1, Socket sock2)
        {
            Console.WriteLine("Thread started " + System.Threading.Thread.CurrentThread.ManagedThreadId);
            threadAmnt++;
            try
            {
                byte[] buffer = new byte[65535];
                int read = 0;
                while (sock1.Connected && sock2.Connected && (read = sock1.Receive(buffer, 0, buffer.Length, SocketFlags.None)) > 0)
                {
                    sock2.Send(buffer, 0, read, SocketFlags.None);
                }

            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            tryClose(sock1);
            tryClose(sock2);
            //Console.WriteLine("Thread ended " + System.Threading.Thread.CurrentThread.ManagedThreadId);
            threadAmnt--;
            Console.WriteLine("Total threads:" + threadAmnt);

        }


        static void handleConnection(Socket conn)
        {

            try
            {
                byte[] buffer = new byte[1024];
                int read = 0;
                int readamnt;
                // keep reading new data until the received data ends with \r\n\r\n
                while (true)
                {
                    readamnt = conn.Receive(buffer, read, 1, SocketFlags.None);
                    read += readamnt;
                    if (read >= 4 && buffer[read - 4] == '\r' && buffer[read - 3] == '\n' && buffer[read - 2] == '\r' && buffer[read - 1] == '\n')
                    {
                        break;
                    }
                    else if (readamnt == 0 || read > buffer.Length)
                    {
                        return;
                    }

                }
                String command = System.Text.Encoding.Default.GetString(buffer);
                if (command.StartsWith("CONNECT"))
                {
                    // this is a CONNECT request, so we need to establish a tunnel
                    // first, we need to parse the host and port from the request
                    string[] parts = command.Split(' ');
                    string[] hostPort = parts[1].Split(':');
                    string host = hostPort[0];
                    int port = int.Parse(hostPort[1]);
                    Console.WriteLine(host + ":" + port.ToString());
                    // now we need to connect to the remote host
                    Socket remote = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    remote.Connect(host, port);

                    // send a 200 OK response to the client
                    conn.Send(System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
                    System.Threading.Thread t = new System.Threading.Thread(() => pipe(conn, remote));
                    t.Start();
                    pipe(remote, conn);

                }
                else
                {
                    conn.Send(System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 500 Internal Server Error\r\n\r\n"));
                    conn.Close();
                }
            }
            catch (SocketException)
            {

            }
        }
        static void Main(string[] args)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(new IPEndPoint(IPAddress.Any, 8080));
            Console.WriteLine("Listening on port 8080");
            sock.Listen(1024);
            while (true)
            {
                Socket conn = sock.Accept();
                Console.WriteLine("New connection from " + conn.RemoteEndPoint.ToString());
                System.Threading.Thread t = new System.Threading.Thread(() => handleConnection(conn));
                t.Start();
            }
        }

    }
}
