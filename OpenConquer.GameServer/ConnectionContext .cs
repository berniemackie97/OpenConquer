using System.Net.Sockets;
using System.Threading.Channels;
using OpenConquer.Protocol.Crypto;

namespace OpenConquer.GameServer
{
    public class ConnectionContext : IAsyncDisposable
    {
        private readonly Channel<byte[]> _sendQueue;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _sendLoop;
        private readonly ILogger<ConnectionContext> _logger;

        public TcpClient TcpClient { get; }
        public NetworkStream Stream => TcpClient.GetStream();

        public CastCfb64Cipher? Cipher { get; set; }

        public ConnectionContext(TcpClient client, ILogger<ConnectionContext> logger)
        {
            TcpClient = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _sendQueue = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });

            _sendLoop = Task.Run(ProcessSendQueueAsync, _cts.Token);
        }

        public async ValueTask SendPacketAsync(byte[] buffer, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (!_sendQueue.Writer.TryWrite(buffer))
            {
                await _sendQueue.Writer.WriteAsync(buffer, ct == default ? _cts.Token : ct).ConfigureAwait(false);
            }
        }

        private async Task ProcessSendQueueAsync()
        {
            try
            {
                await foreach (byte[]? buffer in _sendQueue.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
                {
                    try
                    {
                        await Stream.WriteAsync(buffer, 0, buffer.Length, _cts.Token).ConfigureAwait(false);
                        await Stream.FlushAsync(_cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Network send error on {RemoteEndPoint}", TcpClient.Client.RemoteEndPoint);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Send loop cancelled for {RemoteEndPoint}", TcpClient.Client.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected send loop error for {RemoteEndPoint}", TcpClient.Client.RemoteEndPoint);
            }
        }

        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Disconnecting client {RemoteEndPoint}", TcpClient.Client.RemoteEndPoint);

            _sendQueue.Writer.TryComplete();
            _cts.Cancel();

            try
            {
                await _sendLoop.ConfigureAwait(false);
            }
            catch { }

            try { TcpClient.Close(); } catch { }
            try { TcpClient.Dispose(); } catch { }
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync().ConfigureAwait(false);
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
