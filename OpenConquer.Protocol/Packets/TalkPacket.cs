using System.Buffers;
using System.Buffers.Binary;
using OpenConquer.Domain.Enums;
using OpenConquer.Protocol.Extensions;
using OpenConquer.Protocol.Utilities;

namespace OpenConquer.Protocol.Packets
{
    public readonly struct TalkPacket : IPacket
    {
        public const ushort PacketType = 1004;

        // Color (4) + Type (2) + Time (4) + HearerLookface (4) + SpeakerLookface (4)
        private const int FixedBodyLength = 18;
        private const int HeaderLength = 4;

        public ushort PacketID => PacketType;
        public int Length => HeaderLength + FixedBodyLength + StringPacker.Length;
        public uint Color { get; }
        public ChatType Type { get; }
        public uint Time { get; }
        public uint HearerLookface { get; }
        public uint SpeakerLookface { get; }
        public NetStringPacker StringPacker { get; }

        public TalkPacket(uint color, ChatType type, uint time, uint hearerLookface, uint speakerLookface, NetStringPacker packer)
        {
            Color = color;
            Type = type;
            Time = time;
            HearerLookface = hearerLookface;
            SpeakerLookface = speakerLookface;
            StringPacker = packer;
        }

        public TalkPacket(string words) : this("SYSTEM", "ALLUSERS", words, string.Empty, 0x00ffffffu, ChatType.Talk)
        { }

        public TalkPacket(ChatType type, string words) : this("SYSTEM", "ALLUSERS", words, string.Empty, 0x00ffffffu, type)
        { }

        public TalkPacket(string speaker, string hearer, string words, string emotion, uint color, ChatType type)
        {
            NetStringPacker packer = new(6);
            packer.SetString(0, speaker);
            packer.SetString(1, hearer);
            packer.SetString(2, emotion);
            packer.SetString(3, words);

            packer.SetString(4, string.Empty);
            packer.SetString(5, string.Empty);

            Color = color;
            Type = type;
            Time = 0;
            HearerLookface = SpeakerLookface = 0;
            StringPacker = packer;
        }

        public void FormatWords(string format, params object[] args)
        {
            StringPacker.SetString(3, string.Format(format, args));
        }

        public void Write(IBufferWriter<byte> writer)
        {
            writer.WriteUInt16LittleEndian((ushort)Length);
            writer.WriteUInt16LittleEndian(PacketType);

            writer.WriteUInt32LittleEndian(Color);
            writer.WriteUInt16LittleEndian((ushort)Type);
            writer.WriteUInt32LittleEndian(Time);
            writer.WriteUInt32LittleEndian(HearerLookface);
            writer.WriteUInt32LittleEndian(SpeakerLookface);

            writer.WriteNetStrings(StringPacker);
        }

        public static TalkPacket Parse(ReadOnlySpan<byte> buffer)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2)) != PacketType)
            {
                throw new InvalidOperationException("Not a TalkPacket");
            }

            uint color = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4, 4));
            ChatType type = (ChatType)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(8, 2));
            uint time = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(10, 4));
            uint hearer = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(14, 4));
            uint speaker = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(18, 4));

            NetStringPacker packer = NetStringPacker.Parse(buffer[22..]);

            return new TalkPacket(color, type, time, hearer, speaker, packer);
        }
    }
}
