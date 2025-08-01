using Microsoft.EntityFrameworkCore;
using OpenConquer.AccountServer;
using OpenConquer.AccountServer.Queues;
using OpenConquer.AccountServer.Session;
using OpenConquer.AccountServer.Workers;
using OpenConquer.Domain.Contracts;
using OpenConquer.Infrastructure.POCO;
using OpenConquer.Infrastructure.Persistence.Context;
using OpenConquer.Infrastructure.Services;
using OpenConquer.Infrastructure.Mapping;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.shared.json", optional: false, reloadOnChange: true).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();

builder.Services.Configure<NetworkSettings>(builder.Configuration.GetSection("Network"));

string connString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");
MySqlServerVersion serverVer = new(new Version(8, 0, 36));
builder.Services.AddDbContext<AccountDataContext>(options => options.UseMySql(connString, serverVer));

builder.Services.AddSingleton<ConnectionQueue>();
builder.Services.AddSingleton<ILoginKeyProvider, LockingLoginKeyProvider>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddHostedService<LoginHandshakeService>();
builder.Services.AddHostedService<ConnectionWorker>();

MapsterConfig.RegisterMappings();

IHost host = builder.Build();
host.Run();
