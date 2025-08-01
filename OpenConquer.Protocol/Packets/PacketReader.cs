using System;
using System.Buffers.Binary;
using System.Text;

namespace OpenConquer.Protocol.Packets
{
    public ref struct PacketReader
    {
        private readonly ReadOnlySpan<byte> _span;
        private int _pos;

        public PacketReader(ReadOnlySpan<byte> span)
        {
            _span = span;
            _pos = 0;
        }

        public uint ReadUInt32()
        {
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(_span.Slice(_pos, 4));
            _pos += 4;
            return value;
        }

        public ushort ReadUInt16()
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_span.Slice(_pos, 2));
            _pos += 2;
            return value;
        }

        public string ReadFixedString(int length, Encoding encoding)
        {
            ReadOnlySpan<byte> slice = _span.Slice(_pos, length);
            _pos += length;
            int count = slice.IndexOf((byte)0);
            if (count < 0)
            {
                count = length;
            }

            return encoding.GetString(slice[..count]);
        }
    }
}
