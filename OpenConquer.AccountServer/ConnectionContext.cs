using System.Net.Sockets;
using System.Threading.Channels;
using OpenConquer.Protocol.Crypto;

namespace OpenConquer.AccountServer
{
    public class ConnectionContext : IAsyncDisposable
    {
        private readonly Channel<ArraySegment<byte>> _sendQueue;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _sendLoop;
        private readonly ILogger<ConnectionContext> _logger;

        public TcpClient TcpClient { get; }

        public NetworkStream Stream => TcpClient.GetStream();

        public LoginCipher Cipher { get; } = new();

        public ConnectionContext(TcpClient client, ILogger<ConnectionContext> logger)
        {
            TcpClient = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _sendQueue = Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true
            });

            _sendLoop = Task.Run(ProcessSendQueueAsync);
        }

        public async ValueTask SendPacketAsync(byte[] buffer, int length)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if ((uint)length > (uint)buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            Cipher.Encrypt(buffer, length);

            ArraySegment<byte> segment = new(buffer, 0, length);
            await _sendQueue.Writer.WriteAsync(segment, _cts.Token).ConfigureAwait(false);
        }

        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Disconnecting client {RemoteEndPoint}", TcpClient.Client.RemoteEndPoint);
            _sendQueue.Writer.Complete();
            _cts.Cancel();

            try
            {
                TcpClient.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing TcpClient for {RemoteEndPoint}",
                    TcpClient.Client.RemoteEndPoint);
            }

            await _sendLoop.ConfigureAwait(false);
        }

        private async Task ProcessSendQueueAsync()
        {
            try
            {
                await foreach (ArraySegment<byte> segment in _sendQueue.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
                {
                    try
                    {
                        await Stream.WriteAsync(segment.Array.AsMemory(segment.Offset, segment.Count), _cts.Token)
                                    .ConfigureAwait(false);
                    }
                    catch (Exception ioEx) when (ioEx is not OperationCanceledException)
                    {
                        _logger.LogWarning(ioEx, "I/O error sending to {RemoteEndPoint}",
                            TcpClient.Client.RemoteEndPoint);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Send loop canceled for {RemoteEndPoint}", TcpClient.Client.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in send loop for {RemoteEndPoint}",
                    TcpClient.Client.RemoteEndPoint);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync().ConfigureAwait(false);
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
