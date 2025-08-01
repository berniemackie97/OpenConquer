using System.Buffers;
using System.Text;
using OpenConquer.Protocol.Utilities;

namespace OpenConquer.Protocol.Extensions
{
    public static class BufferWriterExtensions
    {
        public static void WriteUInt16LittleEndian(this IBufferWriter<byte> writer, ushort value)
        {
            Span<byte> span = writer.GetSpan(2);
            span[0] = (byte)(value & 0xFF);
            span[1] = (byte)(value >> 8);
            writer.Advance(2);
        }

        public static void WriteUInt32LittleEndian(this IBufferWriter<byte> writer, uint value)
        {
            Span<byte> span = writer.GetSpan(4);
            span[0] = (byte)(value & 0xFF);
            span[1] = (byte)((value >> 8) & 0xFF);
            span[2] = (byte)((value >> 16) & 0xFF);
            span[3] = (byte)(value >> 24);
            writer.Advance(4);
        }

        public static void WriteNetStrings(this IBufferWriter<byte> writer, NetStringPacker packer)
        {
            int total = packer.Length;
            Span<byte> span = writer.GetSpan(total);

            span[0] = (byte)packer.Count;

            int offs = 1;
            for (int i = 0; i < packer.Count; i++)
            {
                string s = packer.GetString(i) ?? "";
                byte[] bytes = Encoding.Default.GetBytes(s);

                span[offs++] = (byte)bytes.Length;
                bytes.CopyTo(span.Slice(offs, bytes.Length));
                offs += bytes.Length;
            }

            writer.Advance(total);
        }

        public static void Write(this IBufferWriter<byte> writer, ReadOnlySpan<byte> data)
        {
            Span<byte> span = writer.GetSpan(data.Length);
            data.CopyTo(span);
            writer.Advance(data.Length);
        }

        public static void WritePacketHeader(this IBufferWriter<byte> w, int length, ushort packetType)
        {
            w.WriteUInt32LittleEndian((uint)length);
            w.WriteUInt16LittleEndian(packetType);
        }

        public static void WriteFixedString(this IBufferWriter<byte> w, string s, int fixedLength, Encoding enc)
        {
            byte[] bytes = enc.GetBytes(s);
            Span<byte> span = w.GetSpan(fixedLength);
            int cnt = Math.Min(bytes.Length, fixedLength);
            bytes.AsSpan(0, cnt).CopyTo(span);
            if (cnt < fixedLength)
            {
                span[cnt..].Clear();
            }

            w.Advance(fixedLength);
        }
    }

    public static class StringExtensions
    {
        public static string AsFixedLength(this string src, int len)
        {
            if (src.Length == len)
            {
                return src;
            }

            if (src.Length > len)
            {
                return src[..len];
            }

            return src.PadRight(len, '\0');
        }
    }
}
