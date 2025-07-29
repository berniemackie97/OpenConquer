
namespace OpenConquer.Protocol.Packets.Auth
{
    public class SeedResponsePacket(uint seed)
    {
        public const ushort PacketId = 1059;
        public const ushort PacketLength = 8;

        public uint Seed { get; } = seed;

        public byte[] Serialize()
        {
            byte[] buffer = new byte[8];
            BitConverter.GetBytes((ushort)8).CopyTo(buffer, 0);       // Size
            BitConverter.GetBytes((ushort)1059).CopyTo(buffer, 2);    // Type
            BitConverter.GetBytes(Seed).CopyTo(buffer, 4);            // Seed
            return buffer;
        }

    }

}
