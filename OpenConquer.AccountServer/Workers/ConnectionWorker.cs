using System.Net.Sockets;
using OpenConquer.AccountServer.Queues;
using OpenConquer.AccountServer.Session;

namespace OpenConquer.AccountServer.Workers
{
    public class ConnectionWorker(ILogger<ConnectionWorker> logger, IServiceProvider services, ConnectionQueue queue) : BackgroundService
    {
        private readonly ILogger<ConnectionWorker> _logger = logger;
        private readonly IServiceProvider _services = services;
        private readonly ConnectionQueue _queue = queue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConnectionWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _queue.DequeueAsync(stoppingToken);

                    _ = Task.Run(async () =>
                    {
                        using IServiceScope scope = _services.CreateScope();

                        try
                        {
                            LoginClientSession session = ActivatorUtilities.CreateInstance<LoginClientSession>(scope.ServiceProvider, client);

                            await session.HandleHandshakeAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling client session");
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
                    _logger.LogError(ex, "Unhandled exception in connection loop");
                }
            }

            _logger.LogInformation("ConnectionWorker stopped");
        }
    }
}
