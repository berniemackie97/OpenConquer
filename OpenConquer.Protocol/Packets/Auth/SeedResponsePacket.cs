
using System.Buffers;
using OpenConquer.Protocol.Extensions;

namespace OpenConquer.Protocol.Packets.Auth
{
    public readonly struct SeedResponsePacket(uint seed) : IPacket
    {
        private const ushort PacketType = 1059;
        private const int PacketLength = 8;

        public ushort PacketID => PacketType;
        public int Length => PacketLength;
        public uint Seed { get; } = seed;

        public void Write(IBufferWriter<byte> writer)
        {
            writer.WriteUInt16LittleEndian((ushort)Length);
            writer.WriteUInt16LittleEndian(PacketType);
            writer.WriteUInt32LittleEndian(Seed);
        }
    }
}
