using Microsoft.EntityFrameworkCore;
using OpenConquer.Infrastructure.Models;
using OpenConquer.Infrastructure.Persistence.Configuration;

namespace OpenConquer.Infrastructure.Persistence.Context
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<AccountEntity> Accounts { get; set; }
        public DbSet<CharacterEntity> Characters { get; set; }
        public DbSet<LevelStatEntity> LevelStats { get; set; }
        public DbSet<ExperienceCurveEntity> ExperienceCurves { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new AccountEntityConfiguration());
            modelBuilder.ApplyConfiguration(new CharacterEntityConfiguration());
            modelBuilder.ApplyConfiguration(new LevelStatEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ExperienceCurveEntityConfiguration());
        }
    }
}
