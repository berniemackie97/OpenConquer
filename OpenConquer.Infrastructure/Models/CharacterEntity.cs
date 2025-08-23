using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenConquer.Domain.Enums;

namespace OpenConquer.Infrastructure.Models
{
    [Table("Characters")]
    public class CharacterEntity
    {
        [Key]
        [Column("uid")]
        public uint UID { get; set; }

        [Column("account_uid")]
        public uint AccountUID { get; set; }

        [Required, MaxLength(16)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(16)]
        [Column("spouse")]
        public string Spouse { get; set; } = string.Empty;

        [Column("mesh")]
        public uint Mesh { get; set; }

        [Column("hair")]
        public ushort Hair { get; set; }

        [Column("money")]
        public uint Money { get; set; }

        [Column("cp")]
        public uint CP { get; set; }

        [Column("experience", TypeName = "bigint unsigned")]
        public ulong Experience { get; set; }

        [Column("level", TypeName = "tinyint unsigned")]
        public byte Level { get; set; }

        [Column("profession", TypeName = "tinyint unsigned")]
        public Profession Profession { get; set; }

        [Column("metempsychosis", TypeName = "tinyint unsigned")]
        public byte Metempsychosis { get; set; }

        [Column("title", TypeName = "tinyint unsigned")]
        public PlayerTitle Title { get; set; }

        [Column("strength")]
        public ushort Strength { get; set; }

        [Column("agility")]
        public ushort Agility { get; set; }

        [Column("vitality")]
        public ushort Vitality { get; set; }

        [Column("spirit")]
        public ushort Spirit { get; set; }

        [Column("stat_point")]
        public ushort StatPoint { get; set; }

        [Column("health")]
        public int Health { get; set; }

        [Column("mana")]
        public int Mana { get; set; }

        [Column("map_id")]
        public ushort MapID { get; set; }

        [Column("x")]
        public ushort X { get; set; }

        [Column("y")]
        public ushort Y { get; set; }
    }
}
