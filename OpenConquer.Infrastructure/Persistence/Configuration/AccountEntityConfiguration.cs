using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenConquer.Infrastructure.Models;

namespace OpenConquer.Infrastructure.Persistence.Configuration
{
    public class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
    {
        public void Configure(EntityTypeBuilder<AccountEntity> builder)
        {
            builder.ToTable("accounts");
            builder.HasKey(a => a.UID);

            builder.Property(a => a.UID).HasColumnName("uid");
            builder.Property(a => a.Username).HasColumnName("username");
            builder.Property(a => a.Password).HasColumnName("password");
            builder.Property(a => a.Question).HasColumnName("question");
            builder.Property(a => a.Answer).HasColumnName("answer");
            builder.Property(a => a.Permission).HasColumnName("permission");
            builder.Property(a => a.Hash).HasColumnName("hash");
            builder.Property(a => a.Timestamp).HasColumnName("timestamp");
        }
    }
}
