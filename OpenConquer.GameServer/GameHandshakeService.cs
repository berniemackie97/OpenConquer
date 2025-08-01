using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using OpenConquer.GameServer.Queues;
using OpenConquer.Infrastructure.POCO;

namespace OpenConquer.GameServer
{
    public class GameHandshakeService(ILogger<GameHandshakeService> logger, ConnectionQueue queue, IOptions<NetworkSettings> netConfig) : BackgroundService
    {
        private readonly ILogger<GameHandshakeService> _logger = logger;
        private readonly ConnectionQueue _queue = queue;
        private readonly int _gamePort = netConfig.Value.GamePort;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GameHandshakeService listening on port {Port}", _gamePort);
            TcpListener listener = new(IPAddress.Any, _gamePort);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            listener.Start();

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                    _logger.LogInformation("Accepted game connection from {Endpoint}", client.Client.RemoteEndPoint);
                    await _queue.EnqueueAsync(client, stoppingToken);
                }
            }
            catch (OperationCanceledException) 
            {
                // Shutdown
            }
            finally
            {
                listener.Stop();
                _logger.LogInformation("GameHandshakeService stopped");
            }
        }
    }
}
