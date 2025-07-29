using System.Buffers;
using System.Text;
using OpenConquer.Protocol.Extensions;
using OpenConquer.Protocol.Interface;

namespace OpenConquer.Protocol.Packets.Auth
{
    /// <summary>
    /// Mirrors Albetros’s AuthResponsePacket struct:
    /// Size(2) + Type(2) + UID(4) + Key(4) + Port(4) + Hash(4) + ExternalIp[16]
    /// </summary>
    public sealed class AuthResponsePacket : IPacket
    {
        // Response codes (Albetros constants)
        public const uint RESPONSE_INVALID = 1;
        public const uint RESPONSE_VALID = 2;
        public const uint RESPONSE_BANNED = 12;
        public const uint RESPONSE_INVALID_ACCOUNT = 57;

        private const ushort PacketType = 1055;

        public ushort PacketID => PacketType;
        public int Length => 2  // Size field itself
                                 + 2  // Type
                                 + 4  // UID
                                 + 4  // Key
                                 + 4  // Port
                                 + 4  // Hash
                                 + 16; // ExternalIp

        /// <summary>Character UID or ignored on error.</summary>
        public uint UID { get; set; }

        /// <summary>
        /// Response code: one of RESPONSE_INVALID, _VALID, _BANNED, _INVALID_ACCOUNT
        /// </summary>
        public uint Key { get; set; }

        /// <summary>GameServer port the client should connect to on success.</summary>
        public uint Port { get; set; }

        /// <summary>Unused by client; keep zero.</summary>
        public uint Hash { get; set; }

        private readonly byte[] _externalIp = new byte[16];
        public string ExternalIp
        {
            get => Encoding.ASCII.GetString(_externalIp).TrimEnd('\0');
            set
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value ?? "");
                Array.Clear(_externalIp, 0, 16);
                Array.Copy(bytes, _externalIp, Math.Min(bytes.Length, 16));
            }
        }

        public AuthResponsePacket()
        {
            // default to invalid
            Key = RESPONSE_INVALID;
            Hash = 0;
        }

        public void Write(IBufferWriter<byte> writer)
        {
            // 1) Size
            writer.WriteUInt16LittleEndian((ushort)Length);
            // 2) Type
            writer.WriteUInt16LittleEndian(PacketType);
            // 3) UID
            writer.WriteUInt32LittleEndian(UID);
            // 4) Key
            writer.WriteUInt32LittleEndian(Key);
            // 5) Port
            writer.WriteUInt32LittleEndian(Port);
            // 6) Hash
            writer.WriteUInt32LittleEndian(Hash);
            // 7) External IP (16 bytes, ASCII null-padded)
            Span<byte> span = writer.GetSpan(16);
            new Span<byte>(_externalIp).CopyTo(span);
            writer.Advance(16);
        }
    }
}
