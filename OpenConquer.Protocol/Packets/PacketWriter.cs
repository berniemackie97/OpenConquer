using System.Buffers;

namespace OpenConquer.Protocol.Packets
{
    public static class PacketWriter
    {
        public static byte[] Serialize(IPacket packet)
        {
            ArrayBufferWriter<byte> writer = new(packet.Length);
            packet.Write(writer);
            return writer.WrittenSpan.ToArray();
        }
    }
}
