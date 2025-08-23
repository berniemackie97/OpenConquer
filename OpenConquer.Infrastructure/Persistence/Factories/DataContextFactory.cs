using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using OpenConquer.Infrastructure.Persistence.Context;

namespace OpenConquer.Infrastructure.Persistence.Factories
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            string basePath = AppContext.BaseDirectory;
            IConfigurationRoot config = new ConfigurationBuilder().SetBasePath(basePath).AddJsonFile("appsettings.shared.json", optional: false, reloadOnChange: true).AddEnvironmentVariables().Build();

            string conn = config.GetConnectionString("Default") ?? throw new InvalidOperationException("No connection string 'Default'");

            DbContextOptionsBuilder<DataContext> optionsBuilder = new();
            optionsBuilder.UseMySql(conn, new MySqlServerVersion(new Version(8, 0, 36)));

            return new DataContext(optionsBuilder.Options);
        }
    }
}
