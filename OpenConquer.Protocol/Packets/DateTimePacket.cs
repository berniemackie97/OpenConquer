using System.Buffers;
using OpenConquer.Protocol.Extensions;

namespace OpenConquer.Protocol.Packets
{
    public readonly struct DateTimePacket : IPacket
    {
        public const ushort PacketType = 1033;
        public ushort PacketID => PacketType;
        // header (4) + eight 32-bit ints
        public int Length => 4 + (8 * sizeof(int));
        // unused/reserved
        public int Unknown { get; init; }
        public int Year { get; init; }
        public int Month { get; init; }
        public int DayOfYear { get; init; }
        public int Day { get; init; }
        public int Hour { get; init; }
        public int Minute { get; init; }
        public int Second { get; init; }

        public static DateTimePacket Create() => Create(DateTime.Now);

        public static DateTimePacket Create(DateTime now) => new()
        {
            Unknown = 0,
            Year = now.Year - 1900,
            Month = now.Month - 1,
            DayOfYear = now.DayOfYear - 1,
            Day = now.Day,
            Hour = now.Hour,
            Minute = now.Minute,
            Second = now.Second
        };

        public void Write(IBufferWriter<byte> writer)
        {
            writer.WriteUInt16LittleEndian((ushort)Length);
            writer.WriteUInt16LittleEndian(PacketType);

            writer.WriteUInt32LittleEndian((uint)Unknown);
            writer.WriteUInt32LittleEndian((uint)Year);
            writer.WriteUInt32LittleEndian((uint)Month);
            writer.WriteUInt32LittleEndian((uint)DayOfYear);
            writer.WriteUInt32LittleEndian((uint)Day);
            writer.WriteUInt32LittleEndian((uint)Hour);
            writer.WriteUInt32LittleEndian((uint)Minute);
            writer.WriteUInt32LittleEndian((uint)Second);
        }
    }
}
