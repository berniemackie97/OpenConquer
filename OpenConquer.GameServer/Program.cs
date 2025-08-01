using Microsoft.EntityFrameworkCore;
using OpenConquer.Domain.Contracts;
using OpenConquer.GameServer;
using OpenConquer.GameServer.Calculations.Implementation;
using OpenConquer.GameServer.Calculations.Interface;
using OpenConquer.GameServer.Dispatchers;
using OpenConquer.GameServer.Handlers;
using OpenConquer.GameServer.Queues;
using OpenConquer.GameServer.Session.Managers;
using OpenConquer.GameServer.Workers;
using OpenConquer.Infrastructure.Mapping;
using OpenConquer.Infrastructure.Persistence.Context;
using OpenConquer.Infrastructure.POCO;
using OpenConquer.Infrastructure.Services;
using OpenConquer.Protocol.Packets.Parsers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.shared.json", optional: false, reloadOnChange: true).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();

builder.Services.Configure<NetworkSettings>(builder.Configuration.GetSection("Network"));

MapsterConfig.RegisterMappings();

builder.Services.AddDbContext<AccountDataContext>(opts => opts.UseMySql(builder.Configuration.GetConnectionString("Default"), new MySqlServerVersion(new Version(8, 0, 36))));
builder.Services.AddDbContext<GameDataContext>(opts => opts.UseMySql(builder.Configuration.GetConnectionString("Default"), new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddScoped<ILevelStatService, LevelStatService>();

builder.Services.Scan(scan => scan.FromAssemblyOf<IPacketParser>().AddClasses(classes => classes.AssignableTo<IPacketParser>()).AsImplementedInterfaces().WithSingletonLifetime());
builder.Services.Scan(scan => scan.FromAssemblyOf<PacketDispatcher>().AddClasses(classes => classes.AssignableTo(typeof(IPacketHandler<>))).AsImplementedInterfaces().WithTransientLifetime());

builder.Services.AddSingleton<PacketDispatcher>();
builder.Services.AddSingleton<WorldManager>();
builder.Services.AddSingleton<ExperienceService>();
builder.Services.AddSingleton<ConnectionQueue>();
builder.Services.AddSingleton<IExperienceService>(sp => sp.GetRequiredService<ExperienceService>());


builder.Services.AddHostedService(sp => sp.GetRequiredService<ExperienceService>());
builder.Services.AddHostedService<GameHandshakeService>();
builder.Services.AddHostedService<ConnectionWorker>();

IHost host = builder.Build();
host.Run();
