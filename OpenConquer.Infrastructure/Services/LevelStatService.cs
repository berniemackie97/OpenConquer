using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.Infrastructure.Persistence.Context;

namespace OpenConquer.Infrastructure.Services
{
    public class LevelStatService(GameDataContext dataContext) : ILevelStatService
    {
        private readonly GameDataContext _dataContext = dataContext;

        public async Task<IEnumerable<LevelStat>> GetAllAsync(CancellationToken ct = default)
        {
            List<Models.LevelStatEntity> entities = await _dataContext.LevelStats.AsNoTracking().OrderBy(e => e.Profession).ThenBy(e => e.Level).ToListAsync(ct);
            return entities.Adapt<List<LevelStat>>();
        }

        public async Task<LevelStat?> GetAsync(Profession profession, byte level, CancellationToken ct = default)
        {
            Models.LevelStatEntity? entity = await _dataContext.LevelStats.AsNoTracking().FirstOrDefaultAsync(e => e.Profession == profession && e.Level == level, ct);
            return entity?.Adapt<LevelStat>();
        }
    }
}
