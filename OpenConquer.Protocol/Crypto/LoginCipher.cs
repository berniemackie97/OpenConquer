using System;
using OpenConquer.Protocol.Packets;

namespace OpenConquer.Protocol.Crypto
{
    public sealed class LoginCipher : IPacketCipher
    {
        private struct CryptCounter(ushort initial)
        {
            private ushort Counter = initial;

            public byte Key1 => (byte)(Counter & 0xFF);
            public byte Key2 => (byte)(Counter >> 8);
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

        // IPacketCipher implementation

        /// <summary>No key to derive for the login cipher.</summary>
        public void GenerateKeys(object[] seeds)
        {
            // no-op
        }

        /// <summary>Encrypt in-place src→dst.</summary>
        public void Encrypt(Span<byte> src, Span<byte> dst)
        {
            if (dst.Length < src.Length) throw new ArgumentException("dst too small");
            for (int i = 0; i < src.Length; i++)
            {
                byte b = src[i];
                b ^= 0xAB;
                b = (byte)(b >> 4 | b << 4);
                b ^= (byte)(CryptKey1[_encryptCounter.Key1] ^ CryptKey2[_encryptCounter.Key2]);
                _encryptCounter.Increment();
                dst[i] = b;
            }
        }

        /// <summary>Decrypt in-place src→dst.</summary>
        public void Decrypt(Span<byte> src, Span<byte> dst)
        {
            if (dst.Length < src.Length) throw new ArgumentException("dst too small");
            for (int i = 0; i < src.Length; i++)
            {
                byte b = src[i];
                b ^= 0xAB;
                b = (byte)(b >> 4 | b << 4);
                b ^= (byte)(CryptKey2[_decryptCounter.Key2] ^ CryptKey1[_decryptCounter.Key1]);
                _decryptCounter.Increment();
                dst[i] = b;
            }
        }

        public void Encrypt(byte[] buffer, int length) => Encrypt(buffer.AsSpan(0, length), buffer.AsSpan(0, length));

        public void Decrypt(byte[] buffer, int length) => Decrypt(buffer.AsSpan(0, length), buffer.AsSpan(0, length));
    }
}
