using Microsoft.EntityFrameworkCore;
using OpenConquer.Infrastructure.Models;
using OpenConquer.Infrastructure.Persistence.Configuration;

namespace OpenConquer.Infrastructure.Persistence.Context
{
    public class AccountDataContext(DbContextOptions<AccountDataContext> options) : DbContext(options)
    {
        public DbSet<AccountEntity> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new AccountEntityConfiguration());
        }
    }
}
