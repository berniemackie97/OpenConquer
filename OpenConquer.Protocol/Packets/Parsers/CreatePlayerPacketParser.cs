
namespace OpenConquer.Protocol.Packets.Parsers
{
    public class CreatePlayerPacketParser : IPacketParser
    {
        public ushort PacketType => CreatePlayerPacket.PacketType;
        public IPacket Parse(ReadOnlySpan<byte> data) => CreatePlayerPacket.Parse(data);
    }
}
