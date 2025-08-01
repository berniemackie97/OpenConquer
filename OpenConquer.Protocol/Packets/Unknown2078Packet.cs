using System.Buffers;
using System.Buffers.Binary;

namespace OpenConquer.Protocol.Packets
{
    public readonly struct Unknown2078Packet(uint data) : IPacket
    {
        public const ushort PacketType = 2078;
        private const int BodyLength = 4;
        private const int HeaderLength = 4;

        public uint Data { get; } = data;
        public ushort PacketID => PacketType;
        public int Length => HeaderLength + BodyLength;
        public void Write(IBufferWriter<byte> writer)
        {
            Span<byte> hdr = stackalloc byte[4];
            BinaryPrimitives.WriteUInt16LittleEndian(hdr, (ushort)Length);
            BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..], PacketType);
            writer.Write(hdr);

            Span<byte> body = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(body, Data);
            writer.Write(body);
        }

        public static Unknown2078Packet Create(uint data) => new(data);

        public static Unknown2078Packet Parse(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < 8)
            {
                throw new ArgumentException($"Buffer too small ({buffer.Length}) for Unknown2078Packet");
            }

            ushort type = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2));
            if (type != PacketType)
            {
                throw new InvalidOperationException($"Invalid packet type: {type}, expected {PacketType}");
            }

            uint data = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4, 4));
            return new Unknown2078Packet(data);
        }
    }
}
