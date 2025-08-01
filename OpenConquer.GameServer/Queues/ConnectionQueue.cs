using System.Net.Sockets;
using System.Threading.Channels;

namespace OpenConquer.GameServer.Queues
{
    public class ConnectionQueue
    {
        private readonly Channel<TcpClient> _clients = Channel.CreateUnbounded<TcpClient>();
        public ValueTask EnqueueAsync(TcpClient client, CancellationToken ct) => _clients.Writer.WriteAsync(client, ct);
        public ValueTask<TcpClient> DequeueAsync(CancellationToken ct) => _clients.Reader.ReadAsync(ct);
    }
}
