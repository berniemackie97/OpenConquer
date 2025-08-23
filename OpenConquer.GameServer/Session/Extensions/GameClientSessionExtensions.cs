using OpenConquer.GameServer.Session.Managers;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.GameServer.Session.Extensions
{
    public static class GameClientSessionExtensions
    {
        public static Task BroadcastToNearby(this GameClientSession session, IPacket packet, int range = 20)
        {
            WorldManager wm = session.World;
            if (wm.GetPlayer(session.User.UID) is { } self)
            {
                wm.BroadcastToNearby(self, packet, range);
            }
            return Task.CompletedTask;
        }
    }
}
