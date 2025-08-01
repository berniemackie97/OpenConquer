using System.Collections.Concurrent;
using OpenConquer.GameServer.Session.Objects;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.GameServer.Session.Managers
{
    public class WorldManager
    {
        private readonly ConcurrentDictionary<uint, GameCharacter> _players = new();

        public bool AddPlayer(GameCharacter player)
        {
            ArgumentNullException.ThrowIfNull(player);
            return _players.TryAdd(player.UID, player);
        }

        public bool RemovePlayer(uint uid)
        {
            return _players.TryRemove(uid, out _);
        }

        public GameCharacter? GetPlayer(uint uid)
        {
            _players.TryGetValue(uid, out GameCharacter? player);
            return player;
        }

        public IEnumerable<GameCharacter> GetPlayersInMap(ushort mapId)
        {
            return _players.Values.Where(p => p.MapID == mapId);
        }

        public IEnumerable<GameCharacter> GetPlayersNear(GameCharacter origin, int range)
        {
            ArgumentNullException.ThrowIfNull(origin);
            return _players.Values.Where(p => p.MapID == origin.MapID && Math.Abs(p.X - origin.X) <= range && Math.Abs(p.Y - origin.Y) <= range);
        }

        public void BroadcastToNearby(GameCharacter origin, IPacket packet, int range = 20)
        {
            foreach (GameCharacter target in GetPlayersNear(origin, range))
            {
                _ = target.Session.SendAsync(packet, CancellationToken.None);
            }
        }
    }
}
