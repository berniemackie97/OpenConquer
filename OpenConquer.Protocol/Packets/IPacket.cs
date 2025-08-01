using System.Buffers;

namespace OpenConquer.Protocol.Packets
{
    public interface IPacket
    {
        ushort PacketID { get; }
        int Length { get; }
        void Write(IBufferWriter<byte> writer);
    }
}
