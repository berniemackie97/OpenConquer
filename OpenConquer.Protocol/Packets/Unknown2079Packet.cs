using System.Buffers;
using System.Buffers.Binary;

namespace OpenConquer.Protocol.Packets
{
    public readonly struct Unknown2079Packet(uint data) : IPacket
    {
        public const ushort PacketType = 2079;
        private const int BodyLength = 4;
        private const int HeaderLength = 4;

        public ushort PacketID => PacketType;

        public int Length => HeaderLength + BodyLength;

        public uint Data { get; } = data;

        public static Unknown2079Packet Create(uint data) => new(data);

        public static Unknown2079Packet Parse(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < 4 + BodyLength)
            {
                throw new ArgumentException($"Buffer too small for Unknown2079Packet: {buffer.Length} bytes");
            }

            ushort type = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2));
            if (type != PacketType)
            {
                throw new InvalidOperationException($"Invalid packet type: {type}, expected {PacketType}");
            }

            uint data = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4, 4));
            return new Unknown2079Packet(data);
        }

        public void Write(IBufferWriter<byte> writer)
        {
            Span<byte> span = writer.GetSpan(Length);

            BinaryPrimitives.WriteUInt16LittleEndian(span[..2], (ushort)Length);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2, 2), PacketType);

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), Data);

            writer.Advance(Length);
        }
    }
}
