using Microsoft.EntityFrameworkCore;
using OpenConquer.Infrastructure.Models;
using OpenConquer.Infrastructure.Persistence.Configuration;

namespace OpenConquer.Infrastructure.Persistence.Context
{
    public class GameDataContext(DbContextOptions<GameDataContext> options) : DbContext(options)
    {
        public DbSet<CharacterEntity> Characters { get; set; }
        public DbSet<LevelStatEntity> LevelStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CharacterEntityConfiguration());
            modelBuilder.ApplyConfiguration(new LevelStatEntityConfiguration());
        }
    }
}
