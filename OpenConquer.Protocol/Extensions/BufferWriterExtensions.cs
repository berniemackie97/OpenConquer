using System.Buffers;

namespace OpenConquer.Protocol.Extensions
{
    public static class BufferWriterExtensions
    {
        public static void WriteUInt16LittleEndian(this IBufferWriter<byte> writer, ushort value)
        {
            // Reserve 2 bytes
            Span<byte> span = writer.GetSpan(2);
            span[0] = (byte)(value & 0xFF);
            span[1] = (byte)(value >> 8);
            writer.Advance(2);
        }

        public static void WriteUInt32LittleEndian(this IBufferWriter<byte> writer, uint value)
        {
            // Reserve 4 bytes
            Span<byte> span = writer.GetSpan(4);
            span[0] = (byte)(value & 0xFF);
            span[1] = (byte)((value >> 8) & 0xFF);
            span[2] = (byte)((value >> 16) & 0xFF);
            span[3] = (byte)(value >> 24);
            writer.Advance(4);
        }

        public static void Write(this IBufferWriter<byte> writer, ReadOnlySpan<byte> data)
        {
            Span<byte> span = writer.GetSpan(data.Length);
            data.CopyTo(span);
            writer.Advance(data.Length);
        }
    }
}
