using System.Buffers;
using OpenConquer.Domain.Enums;

namespace OpenConquer.Protocol.Packets
{
    public class StatusUpdatePacket : IPacket
    {
        private readonly DataPacket _inner;
        public ushort PacketID => DataPacket.PacketType;
        public int Length => _inner.Length;

        public StatusUpdatePacket(uint playerId, ulong flags)
        {
            uint low = (uint)(flags & 0xFFFFFFFF);
            uint high = (uint)(flags >> 32);

            _inner = DataPacket.Create(playerID: playerId, action: DataAction.MapStatus, a: low, b: high, c: 0, d: 0, e: 0);
        }

        public void Write(IBufferWriter<byte> writer) => _inner.Write(writer);
    }
}
