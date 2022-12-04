using ConnectionUtils;
using System.Net.Sockets;
public class Utils
{
    public static byte[] readExact(SockConnection connection, int amnt)
    {
        byte[] buffer = new byte[amnt];
        int readTotal = 0;
        int read;

        while (readTotal < amnt)
        {
            read = connection.sock.Receive(buffer, readTotal, amnt - readTotal, SocketFlags.None);
            if (read == 0)
            {
                throw new SocketException();
            }
            readTotal += read;
        }
        return buffer;
    }
    public static byte[] readExact(Socket connection, int amnt)
    {
        byte[] buffer = new byte[amnt];
        int readTotal = 0;
        int read;
        while (readTotal < amnt)
        {
            read = connection.Receive(buffer, readTotal, amnt - readTotal, SocketFlags.None);
            if (read == 0)
            {
                throw new SocketException();
            }
            readTotal += read;
        }
        return buffer;
    }

}
