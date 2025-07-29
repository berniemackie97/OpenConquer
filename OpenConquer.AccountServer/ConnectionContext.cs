using System.Net.Sockets;
using System.Threading.Channels;
using OpenConquer.Protocol.Implementation.Crypto;

namespace OpenConquer.AccountServer
{
    /// <summary>
    /// Manages a single client’s socket and a serialized send‐queue using Channels.
    /// </summary>
    public class ConnectionContext
    {
        public TcpClient TcpClient { get; }
        public NetworkStream Stream => TcpClient.GetStream();

        private readonly Channel<ArraySegment<byte>> _sendQueue;
        private readonly CancellationTokenSource _cts = new();

        public LoginCipher Cipher { get; } = new();

        public ConnectionContext(TcpClient client)
        {
            TcpClient = client;
            // single‐writer, single‐reader, unbounded for max throughput
            _sendQueue = Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true
            });
            _ = Task.Run(ProcessSendQueueAsync);
        }

        /// <summary>
        /// Enqueue an encrypted packet to be sent in FIFO order.
        /// </summary>
        public ValueTask SendAsync(byte[] buffer, int length, CancellationToken ct = default)
        {
            var seg = new ArraySegment<byte>(buffer, 0, length);
            return _sendQueue.Writer.WriteAsync(seg, ct);
        }

        /// <summary>
        /// Completes the send‐queue and closes the socket.
        /// </summary>
        public async Task DisconnectAsync()
        {
            _sendQueue.Writer.Complete();
            _cts.Cancel();
            try { TcpClient.Close(); }
            catch { /* ignore */ }
            await Task.CompletedTask;
        }

        private async Task ProcessSendQueueAsync()
        {
            try
            {
                await foreach (var seg in _sendQueue.Reader.ReadAllAsync(_cts.Token))
                {
                    // low‐level send; WriteAsync on the NetworkStream is already async
                    await Stream.WriteAsync(seg.Array.AsMemory(seg.Offset, seg.Count), _cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch { /* swallow any I/O errors */ }
        }
    }
}
