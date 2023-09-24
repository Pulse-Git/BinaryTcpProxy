using System.Net;
using System.Net.Sockets;

namespace BinaryTcpProxy
{
    internal class Program
    {
        public const string ProxyIp = "127.0.0.1";
        public const int ProxyPort = 4589;

        public const int ProxySendDelay = 100;

        public const int MaxMessageSize = 16 * 1024;
        public const int ConnectPort = 7070;

        public const bool NoDelay = true;
        public const int SendTimeout = 0;
        public const int ReceiveTimeout = 0;


        static void Main(string[] args)
        {

            TcpListener listener = new TcpListener(IPAddress.Any, ConnectPort);
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ProxyClient proxyClient = new ProxyClient(client);
                proxyClient.Start();
            }

        }

    }
}