
using OpenConquer.Domain.Enums;

namespace OpenConquer.Domain.Entities
{
    public class Character
    {
        public uint UID { get; set; }
        public string Name { get; set; } = "";
        public string Spouse { get; set; } = "";
        public uint Mesh { get; set; }
        public ushort Hair { get; set; }

        public PlayerPermission Permission { get; set; } = PlayerPermission.Player;

        public uint Money { get; set; }
        public uint CP { get; set; }
        public ulong Experience { get; set; }
        public byte Level { get; set; }
        public byte Profession { get; set; }
        public byte Metempsychosis { get; set; }
        public PlayerTitle Title { get; set; }

        public ushort Strength { get; set; }
        public ushort Agility { get; set; }
        public ushort Vitality { get; set; }
        public ushort Spirit { get; set; }
        public ushort StatPoint { get; set; }

        public int Health { get; set; }
        public int Mana { get; set; }

        public ushort MapID { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
    }
}
