using Microsoft.EntityFrameworkCore;
using OpenConquer.Infrastructure.Models;

namespace OpenConquer.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<AccountEntity> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AccountEntity schema (if needed)
            modelBuilder.Entity<AccountEntity>(entity =>
            {
                entity.ToTable("accounts");

                entity.HasKey(a => a.UID);

                entity.Property(a => a.UID).HasColumnName("uid");
                entity.Property(a => a.Username).HasColumnName("username");
                entity.Property(a => a.Password).HasColumnName("password");
                entity.Property(a => a.Permission).HasColumnName("permission");
            });
        }
    }
}
