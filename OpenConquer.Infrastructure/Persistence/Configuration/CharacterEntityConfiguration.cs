using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenConquer.Infrastructure.Models;

namespace OpenConquer.Infrastructure.Persistence.Configuration
{
    public class CharacterEntityConfiguration : IEntityTypeConfiguration<CharacterEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterEntity> builder)
        {
            builder.ToTable("characters");

            builder.HasKey(c => c.UID);
            builder.Property(c => c.UID).HasColumnName("uid");

            builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(16).IsRequired();
            builder.Property(c => c.Spouse).HasColumnName("spouse").HasMaxLength(16).IsRequired();
            builder.Property(c => c.Mesh).HasColumnName("mesh");
            builder.Property(c => c.Hair).HasColumnName("hair");

            builder.Property(c => c.Money).HasColumnName("money");
            builder.Property(c => c.CP).HasColumnName("cp");
            builder.Property(c => c.Experience).HasColumnName("experience");
            builder.Property(c => c.Level).HasColumnName("level");
            builder.Property(c => c.Profession).HasColumnName("profession");
            builder.Property(c => c.Metempsychosis).HasColumnName("metempsychosis");
            builder.Property(c => c.Title).HasColumnName("title");

            builder.Property(c => c.Strength).HasColumnName("strength");
            builder.Property(c => c.Agility).HasColumnName("agility");
            builder.Property(c => c.Vitality).HasColumnName("vitality");
            builder.Property(c => c.Spirit).HasColumnName("spirit");
            builder.Property(c => c.StatPoint).HasColumnName("stat_point");

            builder.Property(c => c.Health).HasColumnName("health");
            builder.Property(c => c.Mana).HasColumnName("mana");

            builder.Property(c => c.MapID).HasColumnName("map_id");
            builder.Property(c => c.X).HasColumnName("x");
            builder.Property(c => c.Y).HasColumnName("y");
        }
    }
}
