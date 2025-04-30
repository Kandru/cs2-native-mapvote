using System.Net.Sockets;
using System.Text;

namespace NativeMapVote
{
    public class RCONClient(string ip, int port, string password, int timeout = 5000) : IDisposable
    {
        private readonly string ip = ip;
        private readonly int port = port;
        private readonly string password = password;
        private readonly int timeout = timeout;
        private TcpClient? tcpClient;
        private NetworkStream? networkStream;
        private int requestId;

        public void Connect()
        {
            tcpClient = new TcpClient
            {
                ReceiveTimeout = timeout,
                SendTimeout = timeout
            };
            tcpClient.Connect(ip, port);
            networkStream = tcpClient.GetStream();
            Authenticate();
        }

        private void Authenticate()
        {
            requestId++;
            byte[] authPacket = CreatePacket(requestId, 3, password);
            networkStream!.Write(authPacket, 0, authPacket.Length);
            (int RequestId, _, _) = ReceiveResponse();
            if (RequestId == -1)
            {
                throw new Exception("Authentication failed");
            }
        }

        private static byte[] CreatePacket(int requestId, int packetType, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            int packetSize = 4 + 4 + bodyBytes.Length + 2;
            byte[] packet = new byte[4 + packetSize];

            BitConverter.GetBytes(packetSize).CopyTo(packet, 0);
            BitConverter.GetBytes(requestId).CopyTo(packet, 4);
            BitConverter.GetBytes(packetType).CopyTo(packet, 8);
            bodyBytes.CopyTo(packet, 12);
            packet[^2] = 0;
            packet[^1] = 0;

            return packet;
        }

        private (int RequestId, int ResponseType, string Body) ReceiveResponse()
        {
            byte[] lengthBytes = new byte[4];
            _ = networkStream!.Read(lengthBytes, 0, 4);
            int packetLength = BitConverter.ToInt32(lengthBytes, 0);

            byte[] responseBytes = new byte[packetLength];
            _ = networkStream.Read(responseBytes, 0, packetLength);

            int requestId = BitConverter.ToInt32(responseBytes, 0);
            int responseType = BitConverter.ToInt32(responseBytes, 4);
            string body = Encoding.UTF8.GetString(responseBytes, 8, packetLength - 10);

            return (requestId, responseType, body);
        }

        public string SendCommand(string command)
        {
            requestId++;
            byte[] commandPacket = CreatePacket(requestId, 2, command);
            networkStream!.Write(commandPacket, 0, commandPacket.Length);
            (_, _, string Body) = ReceiveResponse();
            return Body;
        }

        public void Close()
        {
            networkStream?.Close();
            tcpClient?.Close();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}