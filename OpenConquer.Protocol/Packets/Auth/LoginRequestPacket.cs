using System.Buffers;
using System.Text;

namespace OpenConquer.Protocol.Packets.Auth
{
    public readonly struct LoginRequestPacket : IPacket
    {
        private const int PacketHeaderSize = 4;
        private const int UsernameOffset = PacketHeaderSize;
        private const int UsernameLength = 16;
        private const int PasswordOffset = 132;
        private const int PasswordLength = 16;

        public ushort PacketID { get; }
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

            ushort length = BitConverter.ToUInt16(decrypted[..2]);
            ushort packetId = BitConverter.ToUInt16(decrypted.Slice(2, 2));

            string username = Encoding.ASCII.GetString(decrypted.Slice(UsernameOffset, UsernameLength)).TrimEnd('\0');

            byte[] blob = decrypted.Slice(PasswordOffset, PasswordLength).ToArray();

            return new LoginRequestPacket(packetId, length, username, blob);
        }

        public void Write(IBufferWriter<byte> writer) => throw new NotSupportedException("LoginRequestPacket is read-only.");
    }
}
