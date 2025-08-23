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
        private readonly IServiceProvider _services;
        private readonly TcpListener _listener;
        private readonly int _port;
        private readonly ConnectionQueue _queue;

        public LoginHandshakeService(ILogger<LoginHandshakeService> logger, IServiceProvider services, IOptions<NetworkSettings> netConfigs, ConnectionQueue queue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _port = netConfigs?.Value.LoginPort ?? throw new ArgumentNullException(nameof(netConfigs));
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _listener.Start();
                _logger.LogInformation("LoginHandshakeService listening on port {Port}", _port);

                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await _listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    EndPoint? endpoint = client.Client.RemoteEndPoint;
                    _logger.LogInformation("Accepted login connection from {Endpoint}", endpoint);

                    await _queue.EnqueueAsync(client, stoppingToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _listener.Stop();
                _logger.LogInformation("LoginHandshakeService stopped listening on port {Port}", _port);
            }
        }

        public override void Dispose()
        {
            _listener.Stop();
            base.Dispose();
        }
    }
}
