using System.Net.Sockets;
namespace ConnectionUtils
{
    public class SockConnection
    {
        public Socket sock { get; }
        public int id { get; }
        public int dataAmount { get; set; }
        public System.Threading.Thread thread;
        public SockConnection()
        {
            sock = null;
        }
        public SockConnection(Socket s, int id)
        {
            sock = s;
            this.id = id;
        }
        public void send(byte[] data)
        {
            sock.Send(data);
            dataAmount += data.Length;
        }
    }

}
