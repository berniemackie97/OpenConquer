using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using OpenConquer.GameServer.Queues;
using OpenConquer.Infrastructure.POCO;

namespace OpenConquer.GameServer
{
    public class GameHandshakeService(ILogger<GameHandshakeService> logger, IServiceProvider services, IOptions<NetworkSettings> netConfig, ConnectionQueue queue) : BackgroundService
    {
        private readonly ILogger<GameHandshakeService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
        private readonly int _gamePort = netConfig?.Value.GamePort ?? throw new ArgumentNullException(nameof(netConfig));
        private readonly ConnectionQueue _queue = queue ?? throw new ArgumentNullException(nameof(queue));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TcpListener listener = new(IPAddress.Any, _gamePort);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            listener.Start();
            _logger.LogInformation("GameHandshakeService listening on port {Port}", _gamePort);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    EndPoint? endpoint = client.Client.RemoteEndPoint;
                    _logger.LogInformation("Accepted game connection from {Endpoint}", endpoint);

                    await _queue.EnqueueAsync(client, stoppingToken).ConfigureAwait(false);
                }
            }
            finally
            {
                listener.Stop();
                _logger.LogInformation("GameHandshakeService stopped listening on port {Port}", _gamePort);
            }
        }
    }
}
