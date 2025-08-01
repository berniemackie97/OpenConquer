using OpenConquer.GameServer.Handlers;
using OpenConquer.GameServer.Session;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.GameServer.Dispatchers
{
    public class PacketDispatcher(IServiceProvider services, ILogger<PacketDispatcher> logger)
    {
        private readonly IServiceProvider _services = services;
        private readonly ILogger<PacketDispatcher> _logger = logger;

        public async Task DispatchAsync(IPacket packet, GameClientSession session, CancellationToken ct)
        {
            Type packetType = packet.GetType();
            Type handlerIface = typeof(IPacketHandler<>).MakeGenericType(packetType);
            Type enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerIface);
            IEnumerable<object> handlers = (IEnumerable<object>?)_services.GetService(enumerableType) ?? [];

            if (!handlers.Any())
            {
                _logger.LogWarning("No handlers registered for packet type {Type}", packetType.Name);
                return;
            }

            foreach (object handler in handlers)
            {
                // could be a better way without reflection, but this is simple
                System.Reflection.MethodInfo m = handlerIface.GetMethod("HandleAsync")!;
                await (Task)m.Invoke(handler, [packet, session, ct])!;
            }
        }
    }
}
