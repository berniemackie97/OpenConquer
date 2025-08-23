using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenConquer.Infrastructure.Models;

namespace OpenConquer.Infrastructure.Persistence.Configuration
{
    public class ExperienceCurveEntityConfiguration : IEntityTypeConfiguration<ExperienceCurveEntity>
    {
        public void Configure(EntityTypeBuilder<ExperienceCurveEntity> builder)
        {
            builder.ToTable("ExperienceCurves");

            builder.HasKey(e => new { e.CurveType, e.Level });

            builder.Property(e => e.CurveType).HasColumnName("curve_type").HasMaxLength(12).IsRequired();
            builder.Property(e => e.Level).HasColumnName("level").HasColumnType("tinyint unsigned").IsRequired();
            builder.Property(e => e.Experience).HasColumnName("experience").IsRequired();
        }
    }
}
