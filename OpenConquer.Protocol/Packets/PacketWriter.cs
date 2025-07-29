using System.Buffers;
using OpenConquer.Protocol.Interface;

namespace OpenConquer.Protocol.Packets
{
    public static class PacketWriter
    {
        /// <summary>
        /// Serializes the given packet to a byte array (header + body).
        /// </summary>
        public static byte[] Serialize(IPacket packet)
        {
            // Use an ArrayBufferWriter to collect the bytes
            ArrayBufferWriter<byte> writer = new(packet.Length);
            packet.Write(writer);
            // Return the written span as an array
            return writer.WrittenSpan.ToArray();
        }
    }
}
