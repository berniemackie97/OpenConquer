
namespace OpenConquer.Protocol.Packets.Parsers
{
    public class DataPacketParser : IPacketParser
    {
        public ushort PacketType => DataPacket.PacketType;
        public IPacket Parse(ReadOnlySpan<byte> data) => DataPacket.Parse(data);
    }
}
