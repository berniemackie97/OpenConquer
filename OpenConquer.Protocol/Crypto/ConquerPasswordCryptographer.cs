using System.Text;

namespace OpenConquer.Protocol.Crypto
{
    public sealed class ConquerPasswordCryptographer
    {
        private readonly byte[] _key = new byte[0x200];

        private static ReadOnlySpan<byte> ScanCodeToVirtualKey =>
        [
            0, 0x1b, 0x31, 50, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0xbd, 0xbb, 8, 9,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 80, 0xdb, 0xdd, 13, 0x11, 0x41, 0x53,
            0x44, 70, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xba, 0xc0, 0xdf, 0x10, 0xde, 90, 0x58, 0x43, 0x56,
            0x42, 0x4e, 0x4d, 0xbc, 190, 0xbf, 0x10, 0x6a, 0x12, 0x20, 20, 0x70, 0x71, 0x72, 0x73, 0x74,
            0x75, 0x76, 0x77, 120, 0x79, 0x90, 0x91, 0x24, 0x26, 0x21, 0x6d, 0x25, 12, 0x27, 0x6b, 0x23,
            40, 0x22, 0x2d, 0x2e, 0x2c, 0, 220, 0x7a, 0x7b, 12, 0xee, 0xf1, 0xea, 0xf9, 0xf5, 0xf3,
            0, 0, 0xfb, 0x2f, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 130, 0x83, 0x84, 0x85, 0x86, 0xed,
            0, 0xe9, 0, 0xc1, 0, 0, 0x87, 0, 0, 0, 0, 0xeb, 9, 0, 0xc2, 0
        ];

        private static ReadOnlySpan<byte> VirtualKeyToScanCode =>
        [
            0, 0, 0, 70, 0, 0, 0, 0, 14, 15, 0, 0, 0x4c, 0x1c, 0, 0,
            0x2a, 0x1d, 0x38, 0, 0x3a, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
            0x39, 0x49, 0x51, 0x4f, 0x47, 0x4b, 0x48, 0x4d, 80, 0, 0, 0, 0x54, 0x52, 0x53, 0x63,
            11, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 0, 0, 0, 0, 0,
            0, 30, 0x30, 0x2e, 0x20, 0x12, 0x21, 0x22, 0x23, 0x17, 0x24, 0x25, 0x26, 50, 0x31, 0x18,
            0x19, 0x10, 0x13, 0x1f, 20, 0x16, 0x2f, 0x11, 0x2d, 0x15, 0x2c, 0x5b, 0x5c, 0x5d, 0, 0x5f,
            0x52, 0x4f, 80, 0x51, 0x4b, 0x4c, 0x4d, 0x47, 0x48, 0x49, 0x37, 0x4e, 0, 0x4a, 0x53, 0x35,
            0x3b, 60, 0x3d, 0x3e, 0x3f, 0x40, 0x41, 0x42, 0x43, 0x44, 0x57, 0x58, 100, 0x65, 0x66, 0x67,
            0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 110, 0x76, 0, 0, 0, 0, 0, 0, 0, 0,
            0x45, 70, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x2a, 0x36, 0x1d, 0x1d, 0x38, 0x38, 0x6a, 0x69, 0x67, 0x68, 0x65, 0x66, 50, 0x20, 0x2e, 0x30,
            0x19, 0x10, 0x24, 0x22, 0x6c, 0x6d, 0x6b, 0x21, 0, 0, 0x27, 13, 0x33, 12, 0x34, 0x35,
            40, 0x73, 0x7e, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1a, 0x56, 0x1b, 0x2b, 0x29,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0x71, 0x5c, 0x7b, 0, 0x6f, 90, 0,
            0, 0x5b, 0, 0x5f, 0, 0x5e, 0, 0, 0, 0x5d, 0, 0x62, 0, 0, 0, 0
        ];

        public ConquerPasswordCryptographer(string username)
        {
            int seed = Encoding.ASCII.GetBytes(username).Sum(b => b);
            LoginPrng rand = new(seed);

            byte[] seedBuffer = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                seedBuffer[i] = (byte)rand.Next();
            }

            for (int i = 1; i < 0x100; i++)
            {
                _key[i * 2] = (byte)i;
                _key[i * 2 + 1] = (byte)(i ^ seedBuffer[i & 15]);
            }

            SortKeyPairs();
        }

        private void SortKeyPairs()
        {
            int pairCount = 0x100;
            for (int i = 1; i < pairCount; i++)
            {
                for (int j = i + 1; j < pairCount; j++)
                {
                    int idxI = i * 2;
                    int idxJ = j * 2;
                    if (_key[idxI + 1] < _key[idxJ + 1])
                    {
                        (_key[idxI], _key[idxJ]) = (_key[idxJ], _key[idxI]);
                        (_key[idxI + 1], _key[idxJ + 1]) = (_key[idxJ + 1], _key[idxI + 1]);
                    }
                }
            }
        }

        public byte[] Decrypt(byte[] data, int length)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (length < 0 || length > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            byte[] output = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byte b = data[i];
                if (b == 0)
                {
                    break;
                }

                int keyIndex = b * 2;
                if (keyIndex < 0 || keyIndex + 1 >= _key.Length)
                {
                    throw new InvalidOperationException("Invalid data byte for decryption.");
                }

                byte index = _key[keyIndex];
                bool isUpper = false;
                if (index >= 0x80)
                {
                    index -= 0x80;
                    isUpper = true;
                }

                byte vk = ScanCodeToVirtualKey[index];
                if (!isUpper && vk is >= 0x41 and <= 0x5A)
                {
                    vk = (byte)(vk + 0x20);
                }

                output[i] = vk;
            }

            return output;
        }

        public byte[] Encrypt(byte[] data, int length)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (length < 0 || length > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            byte[] output = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byte ch = data[i];
                byte vk = ch;
                if (ch is >= 0x61 and <= 0x7A)
                {
                    vk = (byte)(ch - 0x20);
                }

                if (vk < VirtualKeyToScanCode.Length)
                {
                    byte scanCode = VirtualKeyToScanCode[vk];
                    if (ch is >= 0x41 and <= 0x5A)
                    {
                        scanCode = (byte)(scanCode + 0x80);
                    }

                    for (int j = 0; j < 0x100; j++)
                    {
                        if (_key[j * 2] == scanCode)
                        {
                            output[i] = (byte)j;
                            break;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid virtual-key code {vk:X2} for encryption.");
                }
            }

            return output;
        }
    }
}
