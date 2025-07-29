using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OpenConquer.GameServer.Queues
{
    /// <summary>
    /// A simple FIFO for storing TcpClient connections
    /// awaiting processing on the game port.
    /// </summary>
    public class ConnectionQueue
    {
        // unbounded is fine for login/game handshakes
        private readonly Channel<TcpClient> _clients = Channel.CreateUnbounded<TcpClient>();

        /// <summary>
        /// Enqueue a newly accepted TcpClient.
        /// </summary>
        public ValueTask EnqueueAsync(TcpClient client, CancellationToken ct) => _clients.Writer.WriteAsync(client, ct);

        /// <summary>
        /// Dequeue the next TcpClient to service.
        /// </summary>
        public ValueTask<TcpClient> DequeueAsync(CancellationToken ct) => _clients.Reader.ReadAsync(ct);
    }
}
