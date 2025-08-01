using OpenConquer.GameServer.Session;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.GameServer.Handlers
{
    public interface IPacketHandler<TPacket>
        where TPacket : IPacket
    {
        Task HandleAsync(TPacket packet, GameClientSession session, CancellationToken ct);
    }
}
