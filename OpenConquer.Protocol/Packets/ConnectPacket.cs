using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace OpenConquer.Protocol.Packets
{
    // Client -> Server login request (type 1052).
    public readonly struct ConnectPacket : IPacket
    {
        public const ushort PacketType = 1052;
        private const int AccountID = 4;
        private const int DataLength = 4;
        private const int InfoLength = 16;
        private const int BodyLength = AccountID + DataLength + InfoLength;
        private const int HeaderLength = 4;

        public readonly ushort PacketID => PacketType;
        public readonly int Length => HeaderLength + BodyLength;
        public uint AccountId { get; }
        public uint Data { get; }
        public string Info { get; }

        public ConnectPacket(uint accountId, uint data, string info)
        {
            ArgumentNullException.ThrowIfNull(info);
            if (Encoding.Default.GetByteCount(info) > 16)
            {
                throw new ArgumentException("Info may be at most 16 ANSI bytes.", nameof(info));
            }

            AccountId = accountId;
            Data = data;
            Info = info;
        }

        public static ConnectPacket Parse(ReadOnlySpan<byte> buffer)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2)) != PacketType)
            {
                throw new InvalidOperationException("Not a ConnectPacket");
            }

            uint accountId = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4, 4));
            uint data = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(8, 4));

            ReadOnlySpan<byte> infoBytes = buffer.Slice(12, 16);
            string info = Encoding.Default.GetString(infoBytes).TrimEnd('\0');

            return new ConnectPacket(accountId, data, info);
        }

        public readonly void Write(IBufferWriter<byte> writer)
        {
            Span<byte> span = writer.GetSpan(Length);

            BinaryPrimitives.WriteUInt16LittleEndian(span[..2], (ushort)Length);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2, 2), PacketType);

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), AccountId);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), Data);

            byte[] infoBytes = Encoding.Default.GetBytes(Info);
            infoBytes.CopyTo(span.Slice(12, infoBytes.Length));
            // remaining bytes are already zero

            writer.Advance(Length);
        }
    }
}
