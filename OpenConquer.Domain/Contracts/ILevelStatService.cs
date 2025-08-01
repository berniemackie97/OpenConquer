using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;

namespace OpenConquer.Domain.Contracts
{
    public interface ILevelStatService
    {
        Task<IEnumerable<LevelStat>> GetAllAsync(CancellationToken ct = default);
        Task<LevelStat?> GetAsync(Profession profession, byte level, CancellationToken ct = default);
    }
}
