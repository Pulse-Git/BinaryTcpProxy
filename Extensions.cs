using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace BinaryTcpProxy
{
    public static class Extensions
    {

        public static int SafeNetworkRead(this NetworkStream stream, byte[] buffer, int offset, int size)
        {
            try
            {
                return stream.Read(buffer, offset, size);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static bool ExactNetworkRead(this NetworkStream stream, byte[] buffer, int amount)
        {
            int bytesRead = 0;
            while (bytesRead < amount)
            {
                int remaining = amount - bytesRead;
                int result = stream.SafeNetworkRead(buffer, bytesRead, remaining);

                if (result == 0)
                    return false;

                bytesRead += result;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BytesToInt(byte[] bytes)
        {
            return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntToBytes(int value, byte[] bytes, int offset = 0)
        {
            bytes[offset + 0] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }

    }
}