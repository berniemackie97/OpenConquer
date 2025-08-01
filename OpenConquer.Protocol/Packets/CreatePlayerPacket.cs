using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using OpenConquer.Protocol.Extensions;

namespace OpenConquer.Protocol.Packets
{
    // Client -> Server request to create a new character (type 1001).
    public readonly struct CreatePlayerPacket(uint unknown1, uint unknown2, uint unknown3, uint unknown4, uint unknown5, string firstName,
        string lastName, string thirdName, ushort model, ushort job, uint accountId, string macAddress) : IPacket
    {
        public const ushort PacketType = 1001;
        public ushort PacketID => PacketType;
        private const int LengthField = 4;
        private const int PacketIDLength = 2;
        private const int ModelLength = 2;
        private const int JobLength = 2;
        private const int AccountIDLength = 4;
        private const int MacAddressLength = 12;
        public int Length => LengthField + PacketIDLength + (5 * 4) /*Unknown1–5*/ + (3 * 16) /*FirstName, LastName, ThirdName*/ + ModelLength + JobLength + AccountIDLength + MacAddressLength;

        public uint Unknown1 { get; } = unknown1;
        public uint Unknown2 { get; } = unknown2;
        public uint Unknown3 { get; } = unknown3;
        public uint Unknown4 { get; } = unknown4;
        public uint Unknown5 { get; } = unknown5;

        public string FirstName { get; } = firstName ?? throw new ArgumentNullException(nameof(firstName));
        public string LastName { get; } = lastName ?? throw new ArgumentNullException(nameof(lastName));
        public string ThirdName { get; } = thirdName ?? throw new ArgumentNullException(nameof(thirdName));

        public ushort Model { get; } = model;
        public ushort Job { get; } = job;
        public uint AccountId { get; } = accountId;

        public string MacAddress { get; } = macAddress ?? throw new ArgumentNullException(nameof(macAddress));

        public static CreatePlayerPacket Parse(ReadOnlySpan<byte> buffer)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(2, 2)) != PacketType)
            {
                throw new InvalidOperationException($"Not a {nameof(CreatePlayerPacket)}");
            }

            PacketReader reader = new PacketReader(buffer.Slice(6));
            uint u1 = reader.ReadUInt32();
            uint u2 = reader.ReadUInt32();
            uint u3 = reader.ReadUInt32();
            uint u4 = reader.ReadUInt32();
            uint u5 = reader.ReadUInt32();

            string fn = reader.ReadFixedString(16, Encoding.Default);
            string ln = reader.ReadFixedString(16, Encoding.Default);
            string tn = reader.ReadFixedString(16, Encoding.Default);

            ushort model = reader.ReadUInt16();
            ushort job = reader.ReadUInt16();
            uint accountId = reader.ReadUInt32();

            string mac = reader.ReadFixedString(12, Encoding.Default);

            return new CreatePlayerPacket(u1, u2, u3, u4, u5, fn, ln, tn, model, job, accountId, mac);
        }

        public void Write(IBufferWriter<byte> writer)
        {
            writer.WriteUInt16LittleEndian((ushort)Length);
            writer.WriteUInt16LittleEndian(PacketType);

            writer.WriteUInt32LittleEndian(Unknown1);
            writer.WriteUInt32LittleEndian(Unknown2);
            writer.WriteUInt32LittleEndian(Unknown3);
            writer.WriteUInt32LittleEndian(Unknown4);
            writer.WriteUInt32LittleEndian(Unknown5);

            writer.WriteFixedString(FirstName, 16, Encoding.Default);
            writer.WriteFixedString(LastName, 16, Encoding.Default);
            writer.WriteFixedString(ThirdName, 16, Encoding.Default);

            writer.WriteUInt16LittleEndian(Model);
            writer.WriteUInt16LittleEndian(Job);
            writer.WriteUInt32LittleEndian(AccountId);

            writer.WriteFixedString(MacAddress, 12, Encoding.Default);
        }

        public static CreatePlayerPacket Create(uint unknown1, uint unknown2, uint unknown3, uint unknown4, uint unknown5, string firstName,
            string lastName, string thirdName, ushort model, ushort job, uint accountId, string macAddress)
        {
            return new CreatePlayerPacket(unknown1, unknown2, unknown3, unknown4, unknown5, firstName.AsFixedLength(16), lastName.AsFixedLength(16),
                thirdName.AsFixedLength(16), model, job, accountId, macAddress.AsFixedLength(12));
        }
    }
}
