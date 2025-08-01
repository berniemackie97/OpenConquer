using System.Buffers;
using System.Buffers.Binary;
using OpenConquer.Domain.Enums;
using OpenConquer.Protocol.Extensions;

namespace OpenConquer.Protocol.Packets
{
    /// <summary>
    /// Generic data packet: [ Length(2) | Type(2) ]
    ///   [ PlayerId (4) | ParamA (4) | ParamB (4) | Timestamp (4) ]
    ///   [ Action (2) | Direction (2) ]
    ///   [ ParamC (4) | ParamD (4) | ParamE (4) | Flags (1) ]
    /// </summary>
    public readonly struct DataPacket : IPacket
    {
        public const ushort PacketType = 10010;
        public ushort PacketID => PacketType;
        public int Length => HeaderLength
                             + sizeof(uint)   // PlayerId
                             + sizeof(uint)   // ParamA
                             + sizeof(uint)   // ParamB
                             + sizeof(uint)   // Timestamp
                             + sizeof(ushort) // Action
                             + sizeof(ushort) // Direction
                             + sizeof(uint)   // ParamC
                             + sizeof(uint)   // ParamD
                             + sizeof(uint)   // ParamE
                             + sizeof(byte);  // Flags

        private const int HeaderLength = 4; // 2 bytes length + 2 bytes type
        public uint PlayerID { get; init; }
        // First 32-bit parameter (often a map or item ID)
        public uint ParamA { get; init; }
        // Second 32-bit parameter (often unused or conditional)
        public uint ParamB { get; init; }
        public uint Timestamp { get; init; }
        public DataAction Action { get; init; }
        public ushort Direction { get; init; }
        // Third 32-bit parameter (often X coordinate)
        public uint ParamC { get; init; }
        // Fourth 32-bit parameter (often Y coordinate)
        public uint ParamD { get; init; }
        // Fifth 32-bit parameter (unused in some packets)
        public uint ParamE { get; init; }
        // 8-bit flags
        public byte Flags { get; init; }

        public ushort ParamALow
        {
            get => (ushort)ParamA;
            init => ParamA = (uint)((ParamAHigh << 16) | value);
        }
        public ushort ParamAHigh
        {
            get => (ushort)(ParamA >> 16);
            init => ParamA = (uint)((value << 16) | ParamALow);
        }

        public ushort ParamBLow
        {
            get => (ushort)ParamB;
            init => ParamB = (uint)((ParamBHigh << 16) | value);
        }
        public ushort ParamBHigh
        {
            get => (ushort)(ParamB >> 16);
            init => ParamB = (uint)((value << 16) | ParamBLow);
        }

        public ushort ParamCLow
        {
            get => (ushort)ParamC;
            init => ParamC = (uint)((ParamCHigh << 16) | value);
        }
        public ushort ParamCHigh
        {
            get => (ushort)(ParamC >> 16);
            init => ParamC = (uint)((value << 16) | ParamCLow);
        }

        public ushort ParamDLow
        {
            get => (ushort)ParamD;
            init => ParamD = (uint)((ParamDHigh << 16) | value);
        }
        public ushort ParamDHigh
        {
            get => (ushort)(ParamD >> 16);
            init => ParamD = (uint)((value << 16) | ParamDLow);
        }

        public ushort ParamELow
        {
            get => (ushort)ParamE;
            init => ParamE = (uint)((ParamEHigh << 16) | value);
        }
        public ushort ParamEHigh
        {
            get => (ushort)(ParamE >> 16);
            init => ParamE = (uint)((value << 16) | ParamELow);
        }

        public void Write(IBufferWriter<byte> writer)
        {
            writer.WriteUInt16LittleEndian((ushort)Length);
            writer.WriteUInt16LittleEndian(PacketType);

            writer.WriteUInt32LittleEndian(PlayerID);
            writer.WriteUInt32LittleEndian(ParamA);
            writer.WriteUInt32LittleEndian(ParamB);
            writer.WriteUInt32LittleEndian(Timestamp);

            writer.WriteUInt16LittleEndian((ushort)Action);
            writer.WriteUInt16LittleEndian(Direction);

            writer.WriteUInt32LittleEndian(ParamC);
            writer.WriteUInt32LittleEndian(ParamD);
            writer.WriteUInt32LittleEndian(ParamE);

            Span<byte> span = writer.GetSpan(1);
            span[0] = Flags;
            writer.Advance(1);
        }

        public static DataPacket Parse(ReadOnlySpan<byte> buffer)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2)) != PacketType)
            {
                throw new InvalidOperationException($"Not a {nameof(DataPacket)}");
            }

            int offset = HeaderLength;

            uint playerId = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4)); offset += 4;
            uint a = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4)); offset += 4;
            uint b = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4)); offset += 4;
            uint ts = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4)); offset += 4;

            DataAction action = (DataAction)BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2)); offset += 2;
            ushort dir = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2)); offset += 2;

            uint c = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4)); offset += 4;
            uint d = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4)); offset += 4;
            uint e = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4)); offset += 4;

            byte flags = buffer[offset];

            return new DataPacket
            {
                PlayerID = playerId,
                ParamA = a,
                ParamB = b,
                Timestamp = ts,
                Action = action,
                Direction = dir,
                ParamC = c,
                ParamD = d,
                ParamE = e,
                Flags = flags
            };
        }

        public static DataPacket CreateEnterMap(uint characterID, ushort mapId, ushort x, ushort y) => new()
        {
            PlayerID = characterID,
            Action = DataAction.EnterMap,
            ParamCLow = x,
            ParamCHigh = y,
            ParamALow = mapId,  // reuse ParamALow to carry the map ID
            Timestamp = (uint)Environment.TickCount
        };

        public static DataPacket Create(uint playerID, DataAction action, uint a, uint b, uint c, uint d, uint e, byte flags = 0) => new()
        {
            PlayerID = playerID,
            Action = action,
            ParamA = a,
            ParamB = b,
            ParamC = c,
            ParamD = d,
            ParamE = e,
            Flags = flags,
            Timestamp = (uint)Environment.TickCount
        };

        public static DataPacket CreateNinjaStep(uint characterId, ushort mapID, ushort x, ushort y) => new()
        {
            PlayerID = characterId,
            Action = DataAction.NinjaStep,
            ParamALow = mapID,
            ParamCLow = x,
            ParamCHigh = y,
            Timestamp = (uint)Environment.TickCount
        };
    }
}
