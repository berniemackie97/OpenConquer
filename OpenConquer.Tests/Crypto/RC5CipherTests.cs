using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using OpenConquer.Protocol.Implementation.Crypto;

namespace OpenConquer.Tests.Crypto
{
    public class RC5CipherTests
    {
        [Fact]
        public void RC5_ShouldDecryptEncryptedBytesWithSameKey()
        {
            // Arrange
            byte[] key = RC5KeyGenerator.GenerateFromSeed(0x12345678);
            RC5Cipher cipher = new(key);
            byte[] original = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
            byte[] buffer = (byte[])original.Clone();

            // Act
            cipher.Encrypt(buffer);
            cipher.Decrypt(buffer);

            // Assert
            buffer.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void RC5_Decryption_WithWrongKey_ShouldNotProduceOriginal()
        {
            // Arrange
            byte[] key1 = RC5KeyGenerator.GenerateFromSeed(0x12345678);
            byte[] key2 = RC5KeyGenerator.GenerateFromSeed(unchecked((int)0x87654321));

            RC5Cipher cipherEnc = new(key1);
            RC5Cipher cipherDec = new(key2);

            byte[] original = [1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 0, 1, 2];
            byte[] encrypted = cipherEnc.Encrypt(original);

            // Act
            byte[] decrypted = cipherDec.Decrypt(encrypted);

            // Assert
            decrypted.SequenceEqual(original).Should().BeFalse("decryption with the wrong key should not yield the original");
        }


        [Fact]
        public void RC5_Encrypt_ShouldMutateBuffer()
        {
            byte[] key = RC5KeyGenerator.GenerateFromSeed(0x12345678);
            RC5Cipher cipher = new(key);

            byte[] buffer = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
            byte[] original = (byte[])buffer.Clone();

            byte[] result = cipher.Encrypt(buffer);

            result.SequenceEqual(original).Should().BeFalse("encryption should produce a changed buffer");
        }

    }
}
