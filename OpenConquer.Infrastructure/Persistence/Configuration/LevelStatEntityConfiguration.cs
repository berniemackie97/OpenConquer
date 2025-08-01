using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenConquer.Infrastructure.Models;

namespace OpenConquer.Infrastructure.Persistence.Configuration
{
    public class LevelStatEntityConfiguration : IEntityTypeConfiguration<LevelStatEntity>
    {
        public void Configure(EntityTypeBuilder<LevelStatEntity> builder)
        {
            builder.ToTable("LevelStats");

            builder.HasKey(e => e.ID);

            builder.HasIndex(e => new { e.Profession, e.Level }).IsUnique();

            builder.Property(e => e.Profession).HasConversion<int>().IsRequired();

            builder.Property(e => e.Level).HasColumnType("tinyint unsigned").IsRequired();

            builder.Property(e => e.Strength).IsRequired();
            builder.Property(e => e.Agility).IsRequired();
            builder.Property(e => e.Vitality).IsRequired();
            builder.Property(e => e.Spirit).IsRequired();
            builder.Property(e => e.Health).IsRequired();
            builder.Property(e => e.Mana).IsRequired();
        }
    }
}
