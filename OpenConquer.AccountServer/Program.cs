using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenConquer.AccountServer;
using OpenConquer.AccountServer.Mapping;
using OpenConquer.AccountServer.Queues;
using OpenConquer.AccountServer.Session;
using OpenConquer.AccountServer.Workers;
using OpenConquer.Domain.Contracts;
using OpenConquer.Infrastructure.Persistence;
using OpenConquer.Infrastructure.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// ✅ Register MySQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        "Server=localhost;Database=openconquer;User=root;Password=yourpassword;",
        new MySqlServerVersion(new Version(8, 0, 36)) // Adjust version to match your MySQL version
    );
});

// ✅ Register services
builder.Services.AddSingleton<ConnectionQueue>();
builder.Services.AddSingleton<ILoginKeyProvider, LockingLoginKeyProvider>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddHostedService<LoginHandshakeService>();
builder.Services.AddHostedService<ConnectionWorker>();

// ✅ Register Mapster mappings
MapsterConfig.RegisterMappings();

IHost host = builder.Build();
host.Run();
