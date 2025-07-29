using System.Buffers;

namespace OpenConquer.Protocol.Interface
{
    public interface IPacket
    {
        /// <summary>Unique packet ID (as defined by the 5518 protocol).</summary>
        ushort PacketID { get; }

        /// <summary>Total length of the packet, including header.</summary>
        int Length { get; }

        /// <summary>Writes the packet’s bytes (header + body) to the given buffer writer.</summary>
        void Write(IBufferWriter<byte> writer);
    }
}
