using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.Infrastructure.Persistence.Context;

namespace OpenConquer.Infrastructure.Services
{
    public class LevelStatService(IDbContextFactory<DataContext> factory) : ILevelStatService
    {
        private readonly IDbContextFactory<DataContext> _factory = factory;

        public async Task<IEnumerable<LevelStat>> GetAllAsync(CancellationToken ct = default)
        {
            await using DataContext dataContext = _factory.CreateDbContext();
            List<Models.LevelStatEntity> entities = await dataContext.LevelStats.AsNoTracking().OrderBy(e => e.Profession).ThenBy(e => e.Level).ToListAsync(ct);
            return entities.Adapt<List<LevelStat>>();
        }

        public async Task<LevelStat?> GetAsync(Profession profession, byte level, CancellationToken ct = default)
        {
            await using DataContext dataContext = _factory.CreateDbContext();
            Models.LevelStatEntity? entity = await dataContext.LevelStats.AsNoTracking().FirstOrDefaultAsync(e => e.Profession == profession && e.Level == level, ct);
            return entity?.Adapt<LevelStat>();
        }

        public async Task<IEnumerable<ExperienceCurve>> GetAllCurvesAsync(CancellationToken ct = default)
        {
            await using DataContext dataContext = _factory.CreateDbContext();
            List<Models.ExperienceCurveEntity> entities = await dataContext.ExperienceCurves.AsNoTracking().OrderBy(e => e.CurveType).ThenBy(e => e.Level).ToListAsync(ct);
            return entities.Adapt<List<ExperienceCurve>>();
        }

        public async Task<ulong> GetTotalExperienceForLevelAsync(byte level, CancellationToken ct = default)
        {
            await using DataContext dataContext = _factory.CreateDbContext();
            Models.ExperienceCurveEntity? entry = await dataContext.ExperienceCurves.AsNoTracking().Where(e => e.CurveType == "Level").FirstOrDefaultAsync(e => e.Level == level, ct);
            return entry is null ? throw new KeyNotFoundException($"No total‐XP row for level {level}.") : entry.Experience;
        }

        public async Task<uint> GetProficiencyExperienceForTierAsync(byte tier, CancellationToken ct = default)
        {
            await using DataContext dataContext = _factory.CreateDbContext();
            Models.ExperienceCurveEntity? entry = await dataContext.ExperienceCurves.AsNoTracking().Where(e => e.CurveType == "Proficiency").FirstOrDefaultAsync(e => e.Level == tier, ct);
            return entry is null ? throw new KeyNotFoundException($"No proficiency‐XP row for tier {tier}.") : Convert.ToUInt32(entry.Experience);
        }
    }
}
