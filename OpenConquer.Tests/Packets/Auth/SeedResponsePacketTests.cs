using FluentAssertions;
using OpenConquer.Protocol.Implementation.Crypto;
using OpenConquer.Protocol.Packets.Auth;

namespace OpenConquer.Tests.Packets.Auth
{
    public class SeedResponsePacketTests
    {
        [Fact]
        public void Serialize_ShouldProduceExpectedFormat()
        {
            // Arrange
            uint testSeed = 0x12345678;
            SeedResponsePacket packet = new(testSeed);

            // Act
            byte[] bytes = packet.Serialize();

            // Assert
            bytes.Should().HaveCount(8);
            BitConverter.ToUInt16(bytes, 0).Should().Be(8); // Length
            BitConverter.ToUInt16(bytes, 2).Should().Be(1059); // PacketId
            BitConverter.ToUInt32(bytes, 4).Should().Be(testSeed); // Seed value
        }

        [Fact]
        public void EncryptPacket_ShouldReturnEncryptedBytesOfSameLength()
        {
            // Arrange
            uint testSeed = 0x87654321;
            SeedResponsePacket packet = new(testSeed);
            LoginCipher cipher = new();
            byte[] raw = packet.Serialize();

            // Act
            byte[] encrypted = EncryptPacket(raw, cipher);

            // Assert
            encrypted.Should().HaveSameCount(raw);
            encrypted.Should().NotEqual(raw); // Should be different after encryption
        }

        [Fact]
        public void EncryptPacket_ShouldNotMutateOriginalBuffer()
        {
            // Arrange
            uint testSeed = 0xABCDEF01;
            SeedResponsePacket packet = new(testSeed);
            LoginCipher cipher = new();
            byte[] original = packet.Serialize();
            byte[] originalCopy = (byte[])original.Clone();

            // Act
            _ = EncryptPacket(original, cipher);

            // Assert
            original.Should().BeEquivalentTo(originalCopy, "encryption should not mutate input buffer");
        }

        private static byte[] EncryptPacket(byte[] raw, LoginCipher cipher)
        {
            byte[] copy = new byte[raw.Length];
            Buffer.BlockCopy(raw, 0, copy, 0, raw.Length);
            cipher.Encrypt(copy, copy.Length);
            return copy;
        }
    }
}
