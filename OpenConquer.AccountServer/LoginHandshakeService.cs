using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenConquer.AccountServer.Queues;

namespace OpenConquer.AccountServer
{
    public class LoginHandshakeService(ILogger<LoginHandshakeService> logger, ConnectionQueue queue) : BackgroundService
    {
        private readonly ILogger<LoginHandshakeService> _logger = logger;
        private readonly TcpListener _listener = new(IPAddress.Any, 9959);
        private readonly ConnectionQueue _queue = queue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener.Start();
            _logger.LogInformation("LoginHandshakeService listening on port 9959");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _logger.LogInformation("Accepted connection from {Endpoint}", client.Client.RemoteEndPoint);

                    await _queue.EnqueueAsync(client, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting TCP client");
                }
            }
        }

        public override void Dispose()
        {
            _listener.Stop();
            base.Dispose();
        }
    }
}
