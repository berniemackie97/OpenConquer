using OpenConquer.Protocol.Packets;

namespace OpenConquer.Protocol.Crypto
{
    public sealed class LoginCipher : IPacketCipher
    {
        private struct CryptCounter(ushort initial)
        {
            private ushort Counter = initial;

            public readonly byte Key1 => (byte)(Counter & 0xFF);
            public readonly byte Key2 => (byte)(Counter >> 8);
            public void Increment() => Counter++;
        }

        private CryptCounter _encryptCounter;
        private CryptCounter _decryptCounter;
        private static readonly byte[] CryptKey1;
        private static readonly byte[] CryptKey2;

        static LoginCipher()
        {
            CryptKey1 = new byte[0x100];
            CryptKey2 = new byte[0x100];

            byte i_key1 = 0x9D;
            byte i_key2 = 0x62;
            for (int i = 0; i < 0x100; i++)
            {
                CryptKey1[i] = i_key1;
                CryptKey2[i] = i_key2;

                byte t1 = (byte)(i_key1 * 0xFA);
                i_key1 = (byte)((0x0F + t1) * i_key1 + 0x13);

                byte t2 = (byte)(i_key2 * 0x5C);
                i_key2 = (byte)((0x79 - t2) * i_key2 + 0x6D);
            }
        }

        public LoginCipher()
        {
            _encryptCounter = new CryptCounter(0);
            _decryptCounter = new CryptCounter(0);
        }

        public void Encrypt(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                buffer[i] ^= 0xAB;
                buffer[i] = (byte)(buffer[i] >> 4 | buffer[i] << 4);
                buffer[i] ^= (byte)(CryptKey1[_encryptCounter.Key1] ^ CryptKey2[_encryptCounter.Key2]);
                _encryptCounter.Increment();
            }
        }

        public void Decrypt(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                buffer[i] ^= 0xAB;
                buffer[i] = (byte)(buffer[i] >> 4 | buffer[i] << 4);
                buffer[i] ^= (byte)(CryptKey2[_decryptCounter.Key2] ^ CryptKey1[_decryptCounter.Key1]);
                _decryptCounter.Increment();
            }
        }
    }
}
