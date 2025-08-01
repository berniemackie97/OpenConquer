using System.Net.Sockets;
using System.Threading.Channels;

namespace OpenConquer.AccountServer.Queues
{
    public class ConnectionQueue
    {
        private readonly Channel<TcpClient> _channel = Channel.CreateUnbounded<TcpClient>();
        public ValueTask EnqueueAsync(TcpClient client, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(client, cancellationToken);
        public ValueTask<TcpClient> DequeueAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAsync(cancellationToken);
    }
}
