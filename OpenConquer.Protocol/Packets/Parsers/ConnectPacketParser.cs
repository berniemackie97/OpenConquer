
namespace OpenConquer.Protocol.Packets.Parsers
{
    public class ConnectPacketParser : IPacketParser
    {
        public ushort PacketType => ConnectPacket.PacketType;
        public IPacket Parse(ReadOnlySpan<byte> data) => ConnectPacket.Parse(data);
    }
}
