using System.Buffers;
using System.Buffers.Binary;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.Protocol.Utilities;

namespace OpenConquer.Protocol.Packets
{
    public readonly struct SpawnPlayerPacket(uint lookface, uint id, uint guildId, GuildRank guildRank, uint unknown18, ulong effect1, ulong effect2, uint helmetType,
        uint garmentType, uint armorType, uint weaponLType, uint weaponRType, uint accessoryRType, uint accessoryLType, uint mountType, uint unknown70, uint mountArmor,
        ushort life, ushort mobLevel, ushort hair, ushort positionX, ushort positionY, byte direction, byte action, ushort unknown86, uint unknown88, byte rebirth, ushort level,
        byte unknown95, uint away, NobilityType nobility, ushort armorColor, ushort shieldColor, ushort helmetColor, byte mountPlus, uint mountColor, uint quizPoints, bool boss,
        PlayerTitle title, uint helmetArtifactType, uint armorArtifactType, uint weaponRArtifactType, uint weaponLArtifactType, byte profession, NetStringPacker strings) : IPacket
    {
        public const ushort PacketType = 10014;
        private const int HeaderLength = 4;
        private const int FixedLength = 220; // 4-byte header + 216-byte fixed body

        public ushort PacketID => PacketType;
        public int Length => FixedLength + Strings.Length;

        public readonly uint Lookface = lookface;       // +4
        public readonly uint Id = id;             // +8
        public readonly uint GuildId = guildId;        // +12
        public readonly GuildRank GuildRank = guildRank; // +16 (4 bytes)
        public readonly uint Unknown18 = unknown18;      // +18
        public readonly ulong Effect1 = effect1;       // +22
        public readonly ulong Effect2 = effect2;       // +30

        public readonly uint HelmetType = helmetType;     // +40
        public readonly uint GarmentType = garmentType;    // +44
        public readonly uint ArmorType = armorType;      // +48
        public readonly uint WeaponLType = weaponLType;    // +52
        public readonly uint WeaponRType = weaponRType;    // +56
        public readonly uint AccessoryRType = accessoryRType; // +60
        public readonly uint AccessoryLType = accessoryLType; // +64
        public readonly uint MountType = mountType;      // +68
        public readonly uint Unknown70 = unknown70;      // +72
        public readonly uint MountArmor = mountArmor;     // +76

        public readonly ushort Life = life;         // +80
        public readonly ushort MobLevel = mobLevel;     // +82
        public readonly ushort Hair = hair;         // +84
        public readonly ushort PositionX = positionX;    // +86
        public readonly ushort PositionY = positionY;    // +88
        public readonly byte Direction = direction;      // +90
        public readonly byte Action = action;         // +91

        public readonly ushort Unknown86 = unknown86;    // +92
        public readonly uint Unknown88 = unknown88;      // +94
        public readonly byte Rebirth = rebirth;        // +98
        public readonly ushort Level = level;        // +99
        public readonly byte Unknown95 = unknown95;      // +101
        public readonly uint Away = away;           // +102

        public readonly NobilityType Nobility = nobility;   // +119
        public readonly ushort ArmorColor = armorColor;       // +123
        public readonly ushort ShieldColor = shieldColor;      // +125
        public readonly ushort HelmetColor = helmetColor;      // +127

        public readonly byte MountPlus = mountPlus;          // +133
        public readonly uint MountColor = mountColor;         // +139

        public readonly uint QuizPoints = quizPoints;         // +141
        public readonly bool Boss = boss;               // +181

        public readonly PlayerTitle Title = title;       // +167 (2 bytes)
        public readonly uint HelmetArtifactType = helmetArtifactType; // +182
        public readonly uint ArmorArtifactType = armorArtifactType;  // +186
        public readonly uint WeaponRArtifactType = weaponRArtifactType;// +190
        public readonly uint WeaponLArtifactType = weaponLArtifactType;// +194
        public readonly byte Profession = profession;         // +210

        public readonly NetStringPacker Strings = strings; // trailing at +218…

        public static SpawnPlayerPacket Create(Character p)
        {
            NetStringPacker packer = new();
            packer.AddString(p.Name).AddString(string.Empty).AddString(string.Empty);

            return new SpawnPlayerPacket(lookface: p.Mesh, id: p.UID, guildId: 0, guildRank: GuildRank.None, unknown18: 0, effect1: 0, effect2: 0, helmetType: 0, garmentType: 0, armorType: 0,
                weaponLType: 0, weaponRType: 0, accessoryRType: 0, accessoryLType: 0, mountType: 0, unknown70: 0, mountArmor: 0, life: (ushort)p.Health, mobLevel: 0, hair: p.Hair, positionX: p.X,
                positionY: p.Y, direction: 0, action: 0, unknown86: 0, unknown88: 0, rebirth: p.Metempsychosis, level: p.Level, unknown95: 0, away: 0, nobility: NobilityType.Serf, armorColor: 0,
                shieldColor: 0, helmetColor: 0, mountPlus: 0, mountColor: 0, quizPoints: 0, boss: false, title: p.Title, helmetArtifactType: 0, armorArtifactType: 0, weaponRArtifactType: 0,
                weaponLArtifactType: 0, profession: p.Profession, strings: packer);
        }

        public void Write(IBufferWriter<byte> writer)
        {
            int totalLen = Length;
            Span<byte> span = writer.GetSpan(totalLen);

            BinaryPrimitives.WriteUInt16LittleEndian(span[..2], (ushort)totalLen);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2, 2), PacketType);

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), Lookface);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), Id);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12, 4), GuildId);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16, 4), (uint)GuildRank);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(18, 4), Unknown18);
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(22, 8), Effect1);
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(30, 8), Effect2);

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(40, 4), HelmetType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(44, 4), GarmentType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(48, 4), ArmorType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(52, 4), WeaponLType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(56, 4), WeaponRType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(60, 4), AccessoryRType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(64, 4), AccessoryLType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(68, 4), MountType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(72, 4), Unknown70);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(76, 4), MountArmor);

            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(80, 2), Life);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(82, 2), MobLevel);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(84, 2), Hair);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(86, 2), PositionX);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(88, 2), PositionY);
            span[90] = Direction;
            span[91] = Action;

            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(92, 2), Unknown86);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(94, 4), Unknown88);

            span[98] = Rebirth;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(99, 2), Level);
            span[101] = Unknown95;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(102, 4), Away);

            span[119] = (byte)Nobility;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(123, 2), ArmorColor);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(125, 2), ShieldColor);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(127, 2), HelmetColor);

            span[133] = MountPlus;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(139, 4), MountColor);

            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(167, 2), (ushort)Title);

            if (Boss)
            {
                span[181] = 1;
            }

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(182, 4), HelmetArtifactType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(186, 4), ArmorArtifactType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(190, 4), WeaponRArtifactType);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(194, 4), WeaponLArtifactType);

            span[210] = Profession;

            // trailing strings
            byte[] strBytes = Strings.Pack();
            new ReadOnlySpan<byte>(strBytes).CopyTo(span[218..]);

            writer.Advance(totalLen);
        }
    }
}
