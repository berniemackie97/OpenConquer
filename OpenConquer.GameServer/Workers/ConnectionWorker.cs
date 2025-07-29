using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenConquer.GameServer.Queues;
using OpenConquer.GameServer.Session;

namespace OpenConquer.GameServer.Workers
{
    public class ConnectionWorker(ILogger<ConnectionWorker> logger, ConnectionQueue queue, IServiceProvider services) : BackgroundService
    {
        private readonly ILogger<ConnectionWorker> _logger = logger;
        private readonly ConnectionQueue _queue = queue;
        private readonly IServiceProvider _services = services;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GameConnectionWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    System.Net.Sockets.TcpClient client = await _queue.DequeueAsync(stoppingToken);
                    _ = Task.Run(async () =>
                    {
                        using IServiceScope scope = _services.CreateScope();
                        try
                        {
                            // GameClientSession will handle the handshake + further protocol
                            GameClientSession session = ActivatorUtilities.CreateInstance<GameClientSession>(
                                scope.ServiceProvider, client);
                            await session.HandleGameHandshakeAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in game client session");
                        }
                        finally
                        {
                            client.Dispose();
                        }
                    }, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dequeuing game client");
                }
            }

            _logger.LogInformation("GameConnectionWorker stopped");
        }
    }
}
