using System.Buffers;
using System.Text;
using OpenConquer.Protocol.Extensions;

namespace OpenConquer.Protocol.Packets.Auth
{
    public struct AuthResponsePacket : IPacket
    {
        private const ushort PacketLength = 36;
        private const ushort PacketTypeID = 1055;
        private const int ExternalIpLength = 16;

        public const byte RESPONSE_INVALID = 1;
        public const byte RESPONSE_VALID = 2;
        public const byte RESPONSE_BANNED = 12;
        public const byte RESPONSE_INVALID_ACCOUNT = 57;

        public uint UID { get; set; }
        public uint Key { get; set; }
        public uint Port { get; set; }
        public uint Hash { get; set; } = 0;

        public string ExternalIp { get; set; } = string.Empty;

        public AuthResponsePacket() { }

        public static AuthResponsePacket CreateInvalid() => new()
        {
            Key = RESPONSE_INVALID
        };

        public readonly ushort PacketID => PacketTypeID;
        public readonly int Length => PacketLength;

        public readonly void Write(IBufferWriter<byte> writer)
        {
            writer.WriteUInt16LittleEndian(PacketLength);
            writer.WriteUInt16LittleEndian(PacketTypeID);

            writer.WriteUInt32LittleEndian(UID);
            writer.WriteUInt32LittleEndian(Key);
            writer.WriteUInt32LittleEndian(Port);
            writer.WriteUInt32LittleEndian(Hash);

            Span<byte> span = writer.GetSpan(ExternalIpLength);
            span.Clear();

            if (!string.IsNullOrEmpty(ExternalIp))
            {
                byte[] bytes = Encoding.ASCII.GetBytes(ExternalIp);
                bytes.CopyTo(span);
            }
            writer.Advance(ExternalIpLength);
        }
    }

}
