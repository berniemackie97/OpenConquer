namespace OpenConquer.Protocol.Crypto
{
    public sealed class RC5Cipher
    {
        private readonly uint[] _keyBuffer = new uint[4];
        private readonly uint[] _subKeys = new uint[26];

        public RC5Cipher(byte[] key)
        {
            if (key.Length != 16)
            {
                throw new ArgumentException("Key must be exactly 16 bytes.", nameof(key));
            }

            const uint P32 = 0xB7E15163;
            const uint Q32 = 0x61C88647;

            for (int i = 0; i < 4; i++)
            {
                _keyBuffer[i] = BitConverter.ToUInt32(key, i * 4);
            }

            _subKeys[0] = P32;
            for (int i = 1; i < 26; i++)
            {
                _subKeys[i] = _subKeys[i - 1] - Q32;
            }

            uint a = 0, b = 0;
            int iA = 0, iB = 0;
            for (int i = 0; i < 78; i++)
            {
                _subKeys[iA] = RotateLeft(_subKeys[iA] + a + b, 3);
                a = _subKeys[iA];
                iA = (iA + 1) % _subKeys.Length;

                _keyBuffer[iB] = RotateLeft(_keyBuffer[iB] + a + b, (int)(a + b));
                b = _keyBuffer[iB];
                iB = (iB + 1) % _keyBuffer.Length;
            }
        }

        public byte[] Encrypt(byte[] data)
        {
            ValidateInput(data);

            byte[] result = new byte[data.Length];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);

            for (int i = 0; i < result.Length; i += 8)
            {
                uint a = BitConverter.ToUInt32(result, i);
                uint b = BitConverter.ToUInt32(result, i + 4);

                a += _subKeys[0];
                b += _subKeys[1];

                for (int j = 1; j <= 12; j++)
                {
                    a = RotateLeft(a ^ b, (int)b) + _subKeys[2 * j];
                    b = RotateLeft(b ^ a, (int)a) + _subKeys[2 * j + 1];
                }

                Buffer.BlockCopy(BitConverter.GetBytes(a), 0, result, i, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(b), 0, result, i + 4, 4);
            }

            return result;
        }

        public byte[] Decrypt(byte[] data)
        {
            ValidateInput(data);

            byte[] result = new byte[data.Length];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);

            for (int i = 0; i < result.Length; i += 8)
            {
                uint a = BitConverter.ToUInt32(result, i);
                uint b = BitConverter.ToUInt32(result, i + 4);

                for (int j = 12; j >= 1; j--)
                {
                    b = RotateRight(b - _subKeys[2 * j + 1], (int)a) ^ a;
                    a = RotateRight(a - _subKeys[2 * j], (int)b) ^ b;
                }

                b -= _subKeys[1];
                a -= _subKeys[0];

                Buffer.BlockCopy(BitConverter.GetBytes(a), 0, result, i, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(b), 0, result, i + 4, 4);
            }

            return result;
        }

        private static void ValidateInput(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data.Length % 8 != 0 || data.Length == 0)
            {
                throw new ArgumentException("Data length must be a non-zero multiple of 8 bytes.", nameof(data));
            }
        }

        private static uint RotateLeft(uint value, int offset) => value << (offset & 31) | value >> 32 - (offset & 31);

        private static uint RotateRight(uint value, int offset) => value >> (offset & 31) | value << 32 - (offset & 31);
    }
}
