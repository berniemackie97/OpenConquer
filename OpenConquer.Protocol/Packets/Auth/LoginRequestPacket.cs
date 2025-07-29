using System.Buffers;
using System.Text;
using OpenConquer.Protocol.Interface;

namespace OpenConquer.Protocol.Packets.Auth
{
    public sealed class LoginRequestPacket : IPacket
    {
        // Packet layout constants
        private const int PacketHeaderSize = 4; // 2 bytes for Length, 2 for PacketID
        private const int UsernameOffset = PacketHeaderSize; // 4
        private const int UsernameLength = 16;
        private const int PasswordOffset = 132; // ⚠️ this is fixed by CO client layout
        private const int PasswordLength = 16;

        /// <summary>The actual packet ID the client sent (1060 or 1086).</summary>
        public ushort PacketID { get; }

        /// <summary>Total length of this packet, including header.</summary>
        public int Length { get; }

        public string Username { get; }
        public byte[] PasswordBlob { get; }

        private LoginRequestPacket(ushort packetId, int length, string username, byte[] passwordBlob)
        {
            PacketID = packetId;
            Length = length;
            Username = username;
            PasswordBlob = passwordBlob;
        }

        public static LoginRequestPacket Parse(ReadOnlySpan<byte> decrypted)
        {
            if (decrypted.Length < PasswordOffset + PasswordLength)
            {
                throw new ArgumentException("LoginRequestPacket is too short to contain expected fields.");
            }

            ushort length = BitConverter.ToUInt16(decrypted.Slice(0, 2));
            ushort packetId = BitConverter.ToUInt16(decrypted.Slice(2, 2));

            ReadOnlySpan<byte> usernameBytes = decrypted.Slice(UsernameOffset, UsernameLength);
            string username = Encoding.ASCII.GetString(usernameBytes).TrimEnd('\0');

            ReadOnlySpan<byte> passwordBytes = decrypted.Slice(PasswordOffset, PasswordLength);
            byte[] blob = passwordBytes.ToArray();

            return new LoginRequestPacket(packetId, length, username, blob);
        }

        public void Write(IBufferWriter<byte> writer) => throw new NotSupportedException("LoginRequestPacket is read-only.");
    }
}
