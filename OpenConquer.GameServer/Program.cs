using OpenConquer.GameServer;
using OpenConquer.GameServer.Queues;
using OpenConquer.GameServer.Workers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ConnectionQueue>();
builder.Services.AddHostedService<GameHandshakeService>();
builder.Services.AddHostedService<ConnectionWorker>();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();
