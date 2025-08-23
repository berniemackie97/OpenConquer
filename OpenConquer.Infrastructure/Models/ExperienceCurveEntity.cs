using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenConquer.Infrastructure.Models
{
    [Table("ExperienceCurves")]
    public class ExperienceCurveEntity
    {
        [Key, Column(Order = 0)]
        [MaxLength(12)]
        public string CurveType { get; set; } = null!;

        [Key, Column("level", TypeName = "tinyint unsigned", Order = 1)]
        public byte Level { get; set; }

        [Column("experience")]
        public ulong Experience { get; set; }
    }
}
