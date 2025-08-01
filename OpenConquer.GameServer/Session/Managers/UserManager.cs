using System.Collections.Concurrent;

namespace OpenConquer.GameServer.Session.Managers
{
    public class UserManager
    {
        private readonly ConcurrentDictionary<uint, GameClientSession> _online = new();
        public HashSet<uint> NewRoles { get; } = [];
        public bool IsOnline(uint uid) => _online.ContainsKey(uid);
        public GameClientSession Get(uint uid) => _online[uid];

        public void Login(GameClientSession session)
        {
            _online[session.User.UID] = session;
        }

        public void Logout(uint uid)
        {
            _online.TryRemove(uid, out _);
        }
    }
}
