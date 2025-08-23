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
using OpenConquer.Protocol.Crypto;
using OpenConquer.Protocol.Packets.Parsers;
using OpenConquer.Protocol.Utilities;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.shared.json", optional: false, reloadOnChange: true).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();

builder.Services.Configure<NetworkSettings>(builder.Configuration.GetSection("Network"));

builder.Services.AddDbContextFactory<DataContext>(opts => opts.UseMySql(builder.Configuration.GetConnectionString("Default"), new MySqlServerVersion(new Version(8, 0, 36))));

MapsterConfig.RegisterMappings();

builder.Services.Scan(scan => scan.FromAssemblyOf<IPacketParser>().AddClasses(c => c.AssignableTo<IPacketParser>()).AsImplementedInterfaces().WithSingletonLifetime());

builder.Services.AddSingleton<PacketParserRegistry>();

builder.Services.Scan(scan => scan.FromAssemblyOf<PacketDispatcher>().AddClasses(c => c.AssignableTo(typeof(IPacketHandler<>))).AsImplementedInterfaces().WithTransientLifetime());

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();

builder.Services.AddSingleton<ILevelStatService, LevelStatService>();

builder.Services.AddSingleton<UserManager>();
builder.Services.AddSingleton<WorldManager>();

builder.Services.AddSingleton<PacketDispatcher>();

builder.Services.AddSingleton<ExperienceService>();
builder.Services.AddSingleton<IExperienceService>(sp => sp.GetRequiredService<ExperienceService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ExperienceService>());

builder.Services.AddSingleton<ConnectionQueue>();
builder.Services.AddHostedService<ConnectionWorker>();

builder.Services.AddHostedService<GameHandshakeService>();

IHost host = builder.Build();
host.Run();
