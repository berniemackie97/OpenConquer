using OpenConquer.Protocol.Packets;

namespace OpenConquer.Protocol.Crypto
{
    public sealed class SeedCipher(uint seed) : IPacketCipher
    {
        private readonly uint _seed = seed;

        public void Encrypt(byte[] buffer, int length) => Decrypt(buffer, length);

        public void Decrypt(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                byte key = (byte)(_seed >> (i & 3) * 8 & 0xFF);
                buffer[i] ^= key;
            }
        }
    }
}
