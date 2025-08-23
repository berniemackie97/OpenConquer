using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace OpenConquer.Protocol.Crypto
{
    public sealed class CastCfb64Cipher
    {
        private readonly CastCipher _cipher;

        public CastCfb64Cipher(byte[] key)
        {
            _cipher = new CastCipher(CastMode.CFB64);
            lock (key) _cipher.SetKey(key);
        }

        public void Decrypt(byte[] packet)
        {
            var buffer = _cipher.Decrypt(packet);
            lock (packet) Buffer.BlockCopy(buffer, 0, packet, 0, buffer.Length);
        }

        public void Encrypt(byte[] packet)
        {
            var buffer = _cipher.Encrypt(packet);
            lock (packet) Buffer.BlockCopy(buffer, 0, packet, 0, buffer.Length);
        }

        public CastCipher Cipher => _cipher;

        public void SetKey(byte[] key) => _cipher.SetKey(key);

        public void SetIvs(byte[] encryptionIv, byte[] decryptionIv)
        {
            _cipher.EncryptionIV = encryptionIv;
            _cipher.DecryptionIV = decryptionIv;
        }
    }

    public enum CastMode
    {
        ECB,
        CBC,
        CFB64,
        OFB64,
    };

    /// <summary>
    /// Managed implementation using BouncyCastle. Matches OpenSSL CAST-128 semantics.
    /// For CFB64 we use CFB with 8-bit segments (same as OpenSSL's *_cfb64_*).
    /// </summary>
    public sealed class CastCipher : IDisposable
    {
        private readonly CastMode _mode;

        // One cipher per direction to keep independent IV shift-register state.
        private IBufferedCipher _enc;
        private IBufferedCipher _dec;

        private byte[] _key = Array.Empty<byte>();
        private byte[] _encryptionIV = new byte[8];
        private byte[] _decryptionIV = new byte[8];

        public CastCipher(CastMode mode)
        {
            _mode = mode;
            _enc = CreateCipher(forEncryption: true, _encryptionIV);
            _dec = CreateCipher(forEncryption: false, _decryptionIV);
        }

        public void Dispose()
        {
            // nothing unmanaged to free now; keep for API parity
        }

        public void SetKey(byte[] key)
        {
            _key = (byte[])key.Clone();
            // Reset both directions with the current IVs (OpenSSL code also resets counters on SetKey)
            _enc = CreateCipher(true, _encryptionIV);
            _dec = CreateCipher(false, _decryptionIV);
        }

        public byte[] Encrypt(byte[] buffer)
        {
            var output = new byte[buffer.Length];
            // IMPORTANT: do NOT call DoFinal here; we want to keep running state across packets.
            var n = _enc.ProcessBytes(buffer, 0, buffer.Length, output, 0);
            if (n != buffer.Length)
            {
                // For CFB/OFB/CTR this should always be 1:1; safety copy if BC returns less.
                Array.Resize(ref output, n);
            }
            return output;
        }

        public byte[] Decrypt(byte[] buffer)
        {
            var output = new byte[buffer.Length];
            var n = _dec.ProcessBytes(buffer, 0, buffer.Length, output, 0);
            if (n != buffer.Length)
            {
                Array.Resize(ref output, n);
            }
            return output;
        }

        public byte[] EncryptionIV
        {
            get => (byte[])_encryptionIV.Clone();
            set
            {
                if (value == null || value.Length != 8) throw new ArgumentException("IV must be 8 bytes.", nameof(value));
                Buffer.BlockCopy(value, 0, _encryptionIV, 0, 8);
                // Reset ONLY the encryption direction with the new IV (mirrors resetting num=0 in OpenSSL path)
                _enc = CreateCipher(true, _encryptionIV);
            }
        }

        public byte[] DecryptionIV
        {
            get => (byte[])_decryptionIV.Clone();
            set
            {
                if (value == null || value.Length != 8) throw new ArgumentException("IV must be 8 bytes.", nameof(value));
                Buffer.BlockCopy(value, 0, _decryptionIV, 0, 8);
                // Reset ONLY the decryption direction with the new IV
                _dec = CreateCipher(false, _decryptionIV);
            }
        }

        private IBufferedCipher CreateCipher(bool forEncryption, byte[] iv)
        {
            IBlockCipher engine = new Cast5Engine();

            switch (_mode)
            {
                case CastMode.ECB:
                    // No IV in ECB; caller must provide multiples of 8 bytes just like OpenSSL.
                    var ecb = new BufferedBlockCipher(engine);
                    ecb.Init(forEncryption, new KeyParameter(_key));
                    return ecb;

                case CastMode.CBC:
                    var cbc = new BufferedBlockCipher(new CbcBlockCipher(engine));
                    cbc.Init(forEncryption, new ParametersWithIV(new KeyParameter(_key), iv));
                    return cbc;

                case CastMode.CFB64:
                    // OpenSSL's cfb64 = CFB with 8-bit segments over a 64-bit block cipher
                    var cfb8 = new BufferedBlockCipher(new CfbBlockCipher(engine, 8));
                    cfb8.Init(forEncryption, new ParametersWithIV(new KeyParameter(_key), iv));
                    return cfb8;

                case CastMode.OFB64:
                    var ofb64 = new BufferedBlockCipher(new OfbBlockCipher(engine, 64));
                    ofb64.Init(forEncryption, new ParametersWithIV(new KeyParameter(_key), iv));
                    return ofb64;

                default:
                    throw new NotSupportedException($"Mode {_mode} not supported.");
            }
        }
    }
}
