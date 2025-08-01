using System.Buffers;
using System.Buffers.Binary;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.Protocol.Extensions;
using OpenConquer.Protocol.Utilities;

namespace OpenConquer.Protocol.Packets
{
    // fixed 104-byte body + trailing NetStringPacker.
    public readonly struct PlayerInfoPacket(uint id, uint lookface, ushort hair, uint money, uint eMoney, ulong experience, uint unknown1, uint unknown2, uint unknown3,
        uint unknown4, uint unknown5, ushort strength, ushort agility, ushort vitality, ushort spirit, ushort additional, ushort life, ushort mana, short pk, byte level,
        byte profession, byte metempsychosis, byte unknown6, byte unknown7, byte unknown8, byte namesDisplayed, byte unknown18, byte unknown19, uint unknown9, uint unknown10,
        uint unknown11, ushort unknown87, PlayerTitle title, byte unknown90, uint unknown91, uint unknown95, uint unknown99, uint unknown103, byte unknown107, NetStringPacker packer) : IPacket
    {
        public const ushort PacketType = 1006;
        private const int FixedBodyLength = 104;  // header(4) + body(104) = offset 108 for strings
        private const int HeaderLength = 4;


        public ushort PacketID => PacketType;
        public int Length => HeaderLength + FixedBodyLength + StringPacker.Length;
        public uint ID { get; } = id;
        public uint Lookface { get; } = lookface;
        public ushort Hair { get; } = hair;
        public uint Money { get; } = money;
        public uint EMoney { get; } = eMoney;
        public ulong Experience { get; } = experience;
        public uint Unknown1 { get; } = unknown1;
        public uint Unknown2 { get; } = unknown2;
        public uint Unknown3 { get; } = unknown3;
        public uint Unknown4 { get; } = unknown4;
        public uint Unknown5 { get; } = unknown5;
        public ushort Strength { get; } = strength;
        public ushort Agility { get; } = agility;
        public ushort Vitality { get; } = vitality;
        public ushort Spirit { get; } = spirit;
        public ushort AdditionalPoint { get; } = additional;
        public ushort Life { get; } = life;
        public ushort Mana { get; } = mana;
        public short Pk { get; } = pk;
        public byte Level { get; } = level;
        public byte Profession { get; } = profession;
        public byte Metempsychosis { get; } = metempsychosis;
        public byte Unknown6 { get; } = unknown6;
        public byte Unknown7 { get; } = unknown7;
        public byte Unknown8 { get; } = unknown8;
        public byte NamesDisplayed { get; } = namesDisplayed;
        public byte Unknown18 { get; } = unknown18;
        public byte Unknown19 { get; } = unknown19;
        public uint Unknown9 { get; } = unknown9;
        public uint Unknown10 { get; } = unknown10;
        public uint Unknown11 { get; } = unknown11;
        public ushort Unknown87 { get; } = unknown87;
        public PlayerTitle Title { get; } = title;
        public byte Unknown90 { get; } = unknown90;
        public uint Unknown91 { get; } = unknown91;
        public uint Unknown95 { get; } = unknown95;
        public uint Unknown99 { get; } = unknown99;
        public uint Unknown103 { get; } = unknown103;
        public byte Unknown107 { get; } = unknown107;

        public NetStringPacker StringPacker { get; } = packer;

        public static PlayerInfoPacket Create(Character p)
        {
            NetStringPacker packer = new();
            packer.AddString(p.Name);
            packer.AddString(string.Empty);
            packer.AddString(p.Spouse);

            return new PlayerInfoPacket(id: p.UID, lookface: p.Mesh, hair: p.Hair, money: p.Money, eMoney: p.CP, experience: p.Experience, unknown1: 0, unknown2: 0, unknown3: 0, unknown4: 0,
                unknown5: 0, strength: p.Strength, agility: p.Agility, vitality: p.Vitality, spirit: p.Spirit, additional: p.StatPoint, life: (ushort)p.Health, mana: (ushort)p.Mana, pk: 0,
                level: p.Level, profession: p.Profession, metempsychosis: p.Metempsychosis, unknown6: 0, unknown7: 0, unknown8: 0, namesDisplayed: 1, unknown18: 0, unknown19: 0, unknown9: 0,
                unknown10: 0, unknown11: 0, unknown87: 0, title: p.Title, unknown90: 0, unknown91: 0, unknown95: 0, unknown99: 0, unknown103: 0, unknown107: 0, packer: packer);
        }

        public void Write(IBufferWriter<byte> writer)
        {
            writer.WriteUInt16LittleEndian((ushort)Length);
            writer.WriteUInt16LittleEndian(PacketType);

            writer.WriteUInt32LittleEndian(ID);
            writer.WriteUInt32LittleEndian(Lookface);
            writer.WriteUInt16LittleEndian(Hair);
            writer.WriteUInt32LittleEndian(Money);
            writer.WriteUInt32LittleEndian(EMoney);

            Span<byte> sp8 = writer.GetSpan(8);
            BinaryPrimitives.WriteUInt64LittleEndian(sp8, Experience);
            writer.Advance(8);

            writer.WriteUInt32LittleEndian(Unknown1);
            writer.WriteUInt32LittleEndian(Unknown2);
            writer.WriteUInt32LittleEndian(Unknown3);
            writer.WriteUInt32LittleEndian(Unknown4);
            writer.WriteUInt32LittleEndian(Unknown5);

            writer.WriteUInt16LittleEndian(Strength);
            writer.WriteUInt16LittleEndian(Agility);
            writer.WriteUInt16LittleEndian(Vitality);
            writer.WriteUInt16LittleEndian(Spirit);
            writer.WriteUInt16LittleEndian(AdditionalPoint);
            writer.WriteUInt16LittleEndian(Life);
            writer.WriteUInt16LittleEndian(Mana);

            Span<byte> sp2 = writer.GetSpan(2);
            BinaryPrimitives.WriteInt16LittleEndian(sp2, Pk);
            writer.Advance(2);

            writer.GetSpan(1)[0] = Level; writer.Advance(1);
            writer.GetSpan(1)[0] = Profession; writer.Advance(1);
            writer.GetSpan(1)[0] = Metempsychosis; writer.Advance(1);
            writer.GetSpan(1)[0] = Unknown6; writer.Advance(1);
            writer.GetSpan(1)[0] = Unknown7; writer.Advance(1);
            writer.GetSpan(1)[0] = Unknown8; writer.Advance(1);
            writer.GetSpan(1)[0] = NamesDisplayed; writer.Advance(1);
            writer.GetSpan(1)[0] = Unknown18; writer.Advance(1);
            writer.GetSpan(1)[0] = Unknown19; writer.Advance(1);

            writer.WriteUInt32LittleEndian(Unknown9);
            writer.WriteUInt32LittleEndian(Unknown10);
            writer.WriteUInt32LittleEndian(Unknown11);

            writer.WriteUInt16LittleEndian(Unknown87);
            writer.WriteUInt16LittleEndian((ushort)Title);

            writer.GetSpan(1)[0] = Unknown90; writer.Advance(1);

            writer.WriteUInt32LittleEndian(Unknown91);
            writer.WriteUInt32LittleEndian(Unknown95);
            writer.WriteUInt32LittleEndian(Unknown99);
            writer.WriteUInt32LittleEndian(Unknown103);

            writer.GetSpan(1)[0] = Unknown107; writer.Advance(1);

            writer.WriteNetStrings(StringPacker);
        }

        public static PlayerInfoPacket Parse(ReadOnlySpan<byte> buffer)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2)) != PacketType)
            {
                throw new InvalidOperationException("Not a PlayerInfoPacket");
            }

            uint id = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4, 4));
            uint look = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(8, 4));
            ushort hair = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(12, 2));
            uint money = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(14, 4));
            uint eMoney = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(18, 4));
            ulong exp = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(22, 8));
            uint u1 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(30, 4));
            uint u2 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(34, 4));
            uint u3 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(38, 4));
            uint u4 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(42, 4));
            uint u5 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(46, 4));
            ushort str = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(50, 2));
            ushort agi = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(52, 2));
            ushort vit = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(54, 2));
            ushort spi = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(56, 2));
            ushort addp = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(58, 2));
            ushort life = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(60, 2));
            ushort mana = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(62, 2));
            short pk = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(64, 2));
            byte lvl = buffer[66];
            byte prof = buffer[67];
            byte meta = buffer[68];
            byte u6 = buffer[69];
            byte u7 = buffer[70];
            byte u8 = buffer[71];
            byte nd = buffer[72];
            byte u18 = buffer[73];
            byte u19 = buffer[74];
            uint u9 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(75, 4));
            uint u10 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(79, 4));
            uint u11 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(83, 4));
            ushort u87 = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(87, 2));
            PlayerTitle title = (PlayerTitle)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(89, 2));
            byte u90 = buffer[91];
            uint u91 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(92, 4));
            uint u95 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(96, 4));
            uint u99 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(100, 4));
            uint u103 = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(104, 4));
            byte u107 = buffer[108];

            NetStringPacker packer = NetStringPacker.Parse(buffer[108..]);

            return new PlayerInfoPacket(id, look, hair, money, eMoney, exp, u1, u2, u3, u4, u5, str, agi, vit, spi, addp, life,
                mana, pk, lvl, prof, meta, u6, u7, u8, nd, u18, u19, u9, u10, u11, u87, title, u90, u91, u95, u99, u103, u107, packer);
        }
    }
}
