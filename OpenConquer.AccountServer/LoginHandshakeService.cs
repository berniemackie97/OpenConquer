using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using OpenConquer.AccountServer.Queues;
using OpenConquer.Infrastructure.POCO;

namespace OpenConquer.AccountServer
{
    public class LoginHandshakeService : BackgroundService
    {
        private readonly ILogger<LoginHandshakeService> _logger;
        private readonly ConnectionQueue _queue;
        private readonly TcpListener _listener;
        private readonly int _port;

        public LoginHandshakeService(ILogger<LoginHandshakeService> logger, ConnectionQueue queue, IOptions<NetworkSettings> netConfigs)
        {
            _logger = logger;
            _queue = queue;
            _port = netConfigs.Value.LoginPort;
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            StartListener();
            _logger.LogInformation("LoginHandshakeService is listening on port {Port}", _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    EndPoint? endpoint = client.Client.RemoteEndPoint;
                    _logger.LogInformation("Accepted login connection from {Endpoint}", endpoint);

                    await _queue.EnqueueAsync(client, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("LoginHandshakeService cancellation requested.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to accept incoming login connection");
                }
            }
        }

        public override void Dispose()
        {
            _logger.LogInformation("Stopping LoginHandshakeService listener on port {Port}", _port);
            _listener.Stop();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        private void StartListener()
        {
            try
            {
                _listener.Start();
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Unable to start TCP listener on port {Port}", _port);
                throw;
            }
        }
    }
}
