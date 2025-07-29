using System.Net;
using System.Net.Sockets;
using OpenConquer.GameServer.Queues;

namespace OpenConquer.GameServer
{
    public class GameHandshakeService(ILogger<GameHandshakeService> logger, ConnectionQueue queue, IConfiguration config) : BackgroundService
    {
        private readonly ILogger<GameHandshakeService> _logger = logger;
        private readonly ConnectionQueue _queue = queue;
        private readonly int _gamePort = config.GetValue<int>("GamePort");

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
                    _logger.LogInformation("Accepted game connection from {Endpoint}",
                                           client.Client.RemoteEndPoint);
                    await _queue.EnqueueAsync(client, stoppingToken);
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }
            finally
            {
                listener.Stop();
                _logger.LogInformation("GameHandshakeService stopped");
            }
        }
    }
}
