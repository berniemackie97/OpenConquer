using OpenConquer.Domain.Entities;
using OpenConquer.Protocol.Packets;
using OpenConquer.Domain.Enums;
using OpenConquer.GameServer.Calculations.Interface;
using OpenConquer.GameServer.Session.Extensions;

namespace OpenConquer.GameServer.Session.Objects
{
    public class GameCharacter(Character domain, GameClientSession session, IExperienceService experienceService)
    {
        public Character Domain { get; } = domain ?? throw new ArgumentNullException(nameof(domain));
        public GameClientSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

        private readonly Dictionary<Effect1, Stat1> _activeStat1 = [];
        private readonly Dictionary<Effect2, Stat2> _activeStat2 = [];
        private readonly IExperienceService _experienceService = experienceService ?? throw new ArgumentNullException(nameof(experienceService));

        public uint UID => Domain.UID;
        public string Name => Domain.Name;
        public ushort X { get => Domain.X; private set => Domain.X = value; }
        public ushort Y { get => Domain.Y; private set => Domain.Y = value; }
        public ushort MapID { get => Domain.MapID; private set => Domain.MapID = value; }

        public byte Level { get => Domain.Level; private set => Domain.Level = value; }
        public byte Profession => Domain.Profession;
        public uint Mesh { get => Domain.Mesh; private set => Domain.Mesh = value; }
        public ushort Hair { get => Domain.Hair; private set => Domain.Hair = value; }

        public int Health { get => Domain.Health; set => Domain.Health = value; }
        public int Mana { get => Domain.Mana; set => Domain.Mana = value; }

        public int MaxHealth => _experienceService.GetMaxHealth(Profession, Level);
        public int MaxMana => _experienceService.GetMaxMana(Profession, Level);

        public void NinjaStep(ushort x, ushort y)
        {
            X = x;
            Y = y;
            DataPacket pkt = DataPacket.CreateNinjaStep(UID, MapID, x, y);
            Session.BroadcastToNearby(pkt);
        }

        public async Task CompleteLoginAsync(CancellationToken ct)
        {
            await Session.SendAsync(PlayerInfoPacket.Create(Domain), ct);
            await Session.SendAsync(DataPacket.CreateEnterMap(UID, MapID, X, Y), ct);
            await Session.SendAsync(DateTimePacket.Create(), ct);
        }

        public void AddEffect1(Effect1 type, int durationMs)
        {
            _activeStat1[type] = new Stat1(type, durationMs);
            UpdateStatus1();
        }

        public void RemoveEffect1(Effect1 type)
        {
            if (_activeStat1.Remove(type))
            {
                UpdateStatus1();
            }
        }

        private void UpdateStatus1()
        {
            ulong flags = _activeStat1.Values.Aggregate(0UL, (acc, s) => acc | (ulong)s.Flag);

            _ = Session.SendAsync(new StatusUpdatePacket(UID, flags), CancellationToken.None);
            _ = Session.BroadcastToNearby(new StatusUpdatePacket(UID, flags));
        }

        public void AddEffect2(Effect2 type, int durationMs)
        {
            _activeStat2[type] = new Stat2(type, durationMs);
            UpdateStatus2();
        }

        public void RemoveEffect2(Effect2 type)
        {
            if (_activeStat2.Remove(type))
            {
                UpdateStatus2();
            }
        }

        private void UpdateStatus2()
        {
            ulong flags = _activeStat2.Values.Aggregate(0UL, (acc, s) => acc | (ulong)s.Flag);

            _ = Session.SendAsync(new StatusUpdatePacket(UID, flags), CancellationToken.None);
            _ = Session.BroadcastToNearby(new StatusUpdatePacket(UID, flags));
        }
    }

    internal readonly struct Stat1(Effect1 flag, int durationMs)
    {
        public Effect1 Flag { get; } = flag;
        public DateTime Expires { get; } = DateTime.UtcNow.AddMilliseconds(durationMs);
    }

    internal readonly struct Stat2(Effect2 flag, int durationMs)
    {
        public Effect2 Flag { get; } = flag;
        public DateTime Expires { get; } = DateTime.UtcNow.AddMilliseconds(durationMs);
    }
}
