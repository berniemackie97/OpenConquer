using System.Buffers;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.Protocol.Extensions
{
    public static class PacketExtensions
    {
        public static byte[] ToBytes(this IPacket packet)
        {
            ArrayBufferWriter<byte> writer = new ArrayBufferWriter<byte>();
            packet.Write(writer);
            return writer.WrittenSpan.ToArray();
        }
    }
}
