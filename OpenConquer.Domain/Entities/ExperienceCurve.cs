
namespace OpenConquer.Domain.Entities
{
    public class ExperienceCurve
    {
        public string CurveType { get; set; } = null!;
        public byte Level { get; set; }
        public ulong Experience { get; set; }
    }
}
