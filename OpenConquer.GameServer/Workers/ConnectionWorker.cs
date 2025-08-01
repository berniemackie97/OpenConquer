using System.Net.Sockets;
using OpenConquer.GameServer.Queues;
using OpenConquer.GameServer.Session;

namespace OpenConquer.GameServer.Workers
{
    public class ConnectionWorker(ILogger<ConnectionWorker> logger, ConnectionQueue queue, IServiceProvider services) : BackgroundService
    {
        private readonly ILogger<ConnectionWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConnectionQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started", nameof(ConnectionWorker));

            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient? client = null;
                try
                {
                    client = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
                    System.Net.EndPoint? endpoint = client.Client.RemoteEndPoint;
                    _logger.LogInformation("Dequeued game client connection from {Endpoint}", endpoint);

                    _ = Task.Run(() => HandleClientSessionAsync(client, stoppingToken), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dequeuing game client");
                    client?.Dispose();
                }
            }

            _logger.LogInformation("{Service} stopped", nameof(ConnectionWorker));
        }

        private async Task HandleClientSessionAsync(TcpClient client, CancellationToken ct)
        {
            using IServiceScope scope = _services.CreateScope();
            try
            {
                GameClientSession session = ActivatorUtilities.CreateInstance<GameClientSession>(scope.ServiceProvider, client);
                await session.HandleGameHandshakeAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during game client session for {Endpoint}", client.Client.RemoteEndPoint);
            }
            finally
            {
                client.Dispose();
                _logger.LogInformation("Closed connection for {Endpoint}", client.Client.RemoteEndPoint);
            }
        }
    }
}
