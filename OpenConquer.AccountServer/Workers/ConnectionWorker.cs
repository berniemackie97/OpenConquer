using System.Net.Sockets;
using OpenConquer.AccountServer.Queues;
using OpenConquer.AccountServer.Session;

namespace OpenConquer.AccountServer.Workers
{
    public class ConnectionWorker(ILogger<ConnectionWorker> logger, ConnectionQueue queue, IServiceProvider services) : BackgroundService
    {
        private readonly ILogger<ConnectionWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConnectionQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConnectionWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = null!;
                try
                {
                    client = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
                    _logger.LogDebug("Dequeued new client from queue: {RemoteEndPoint}", client.Client.RemoteEndPoint);

                    _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ConnectionWorker loop");
                }
            }

            _logger.LogInformation("ConnectionWorker stopped");
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using IServiceScope scope = _services.CreateScope();
            try
            {
                LoginClientSession session = ActivatorUtilities.CreateInstance<LoginClientSession>(scope.ServiceProvider, client);

                _logger.LogInformation("Beginning handshake for {RemoteEndPoint}", client.Client.RemoteEndPoint);

                await session.HandleHandshakeAsync(ct).ConfigureAwait(false);

                _logger.LogInformation("Completed handshake for {RemoteEndPoint}", client.Client.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception handling client {RemoteEndPoint}", client.Client.RemoteEndPoint);
            }
            finally
            {
                try
                {
                    client.Close();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing client {RemoteEndPoint}", client.Client.RemoteEndPoint);
                }
            }
        }
    }
}
