using System.Collections.Concurrent;
using System.Net.Sockets;

namespace BinaryTcpProxy
{
    public sealed class ProxyClient
    {
        ConcurrentQueue<ArraySegment<byte>> SendToProxy;
        ConcurrentQueue<ArraySegment<byte>> SendFromProxy;

        TcpClient Client;
        Thread ThreadReceive;
        Thread ThreadSendBack;

        TcpClient ProxyTcpClient;
        Thread ProxyThread;
        Thread ProxySendThread;

        public ProxyClient(TcpClient client)
        {
            this.Client = client;
            this.SendToProxy = new ConcurrentQueue<ArraySegment<byte>>();
            this.SendFromProxy = new ConcurrentQueue<ArraySegment<byte>>();
        }

        public void Start()
        {
            NetworkStream clientStream = Client.GetStream();
            ThreadSendBack = SendProxy(this, Client, clientStream, SendFromProxy);
            ThreadReceive = ReadProxy(this, Client, clientStream, SendToProxy);

            ProxyTcpClient = new TcpClient();
            ProxyTcpClient.Connect(Program.ProxyIp, Program.ProxyPort);
            ProxyTcpClient.NoDelay = Program.NoDelay;
            ProxyTcpClient.SendTimeout = Program.SendTimeout;
            ProxyTcpClient.ReceiveTimeout = Program.ReceiveTimeout;

            NetworkStream proxyStream = ProxyTcpClient.GetStream();
            ProxySendThread = SendProxy(this, ProxyTcpClient, proxyStream, SendToProxy);
            ProxyThread = ReadProxy(this, ProxyTcpClient, proxyStream, SendFromProxy);
        }

        void Stop()
        {
            Client.Close();
            ProxyTcpClient.Close();
        }

        public static Thread SendProxy(ProxyClient proxyClient, TcpClient client, NetworkStream stream, ConcurrentQueue<ArraySegment<byte>> from)
        {
            if (Program.ProxySendDelay <= 0)
            {
                Thread SendThread = new Thread(() =>
                {
                    try
                    {
                        while (client.Connected)
                        {
                            if (from.TryDequeue(out ArraySegment<byte> result))
                            {
                                SendMessages(stream, result.Array, result.Count);
                            }
                        }
                    }
                    catch (Exception e)
                    {

                    }
                    finally
                    {
                        proxyClient.Stop();
                    }

                });
                SendThread.IsBackground = true;
                SendThread.Start();
                return SendThread;
            } else
            {
                Thread SendThread = new Thread(() =>
                {
                    object sendLock = new object();
                    try
                    {
                        while (client.Connected)
                        {
                            if (from.TryDequeue(out ArraySegment<byte> result))
                            {
                                Task.Run(async () =>
                                {
                                    ArraySegment<byte> delayed = result;
                                    Thread.Sleep(TimeSpan.FromMilliseconds(Program.ProxySendDelay));
                                    lock (sendLock)
                                        SendMessages(stream, delayed.Array, delayed.Count);
                                });
                                
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        proxyClient.Stop();
                    }

                });
                SendThread.IsBackground = true;
                SendThread.Start();
                return SendThread;
            }
        }

        public static Thread ReadProxy(ProxyClient proxyClient, TcpClient client, NetworkStream stream, ConcurrentQueue<ArraySegment<byte>> into)
        {
            Thread ReadThread = new Thread(() => {
                byte[] receiveBuffer = new byte[4 + Program.MaxMessageSize];
                byte[] headerBuffer = new byte[4];

                try
                {
                    while (client.Connected)
                    {
                        if (!ReadNextMessage(stream, Program.MaxMessageSize, headerBuffer, receiveBuffer, out int size))
                            break;

                        ArraySegment<byte> message = new ArraySegment<byte>(receiveBuffer, 0, size);

                        byte[] payload = new byte[4 + size];
                        int position = 0;

                        Extensions.IntToBytes(size, payload, position);
                        position += 4;

                        Buffer.BlockCopy(receiveBuffer, 0, payload, position, size);
                        position += size;

                        into.Enqueue(new ArraySegment<byte>(payload, 0, position));
                    }

                }
                catch (Exception e)
                {

                }
                finally
                {
                    proxyClient.Stop();
                }
            });
            ReadThread.IsBackground = true;
            ReadThread.Start();
            return ReadThread;
        }

        public static bool SendMessages(NetworkStream stream, byte[] payload, int packetSize)
        {
            try
            {
                stream.Write(payload, 0, packetSize);
                return true;
            }
            catch (Exception exception)
            {
                return false;
            }
        }

        public static bool ReadNextMessage(NetworkStream stream, int MaxMessageSize, byte[] headerBuffer, byte[] payloadBuffer, out int size)
        {
            size = 0;

            if (payloadBuffer.Length != 4 + MaxMessageSize)
            {
                return false;
            }

            if (!stream.ExactNetworkRead(headerBuffer, 4))
                return false;

            size = Extensions.BytesToInt(headerBuffer);

            if (size > 0 && size <= MaxMessageSize)
                return stream.ExactNetworkRead(payloadBuffer, size);

            return false;
        }

    }
}