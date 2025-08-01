namespace OpenConquer.Protocol.Packets.Parsers
{
    public interface IPacketParser
    {
        ushort PacketType { get; }

        IPacket Parse(ReadOnlySpan<byte> data);
    }

    public class PacketParserRegistry(IEnumerable<IPacketParser> parsers)
    {
        private readonly Dictionary<ushort, IPacketParser> _parsers = parsers.ToDictionary(p => p.PacketType);

        public IPacket ParsePacket(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4)
            {
                throw new InvalidOperationException("Packet too short to contain header");
            }

            ushort type = BitConverter.ToUInt16(data.Slice(2, 2));

            if (!_parsers.TryGetValue(type, out IPacketParser? parser))
            {
                throw new InvalidOperationException($"Unknown packet type {type}");
            }

            return parser.Parse(data);
        }
    }
}
