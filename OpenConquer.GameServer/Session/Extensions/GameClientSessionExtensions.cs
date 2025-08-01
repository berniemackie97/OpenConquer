using OpenConquer.GameServer.Session.Managers;
using OpenConquer.GameServer.Session.Objects;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.GameServer.Session.Extensions
{
    public static class GameClientSessionExtensions
    {
        public static Task BroadcastToNearby(this GameClientSession session, IPacket packet, int range = 20)
        {
            WorldManager wm = session.World;
            GameCharacter? self = wm.GetPlayer(session.User.UID);
            if (self != null)
            {
                wm.BroadcastToNearby(self, packet, range);
            }
            return Task.CompletedTask;
        }
    }
}
