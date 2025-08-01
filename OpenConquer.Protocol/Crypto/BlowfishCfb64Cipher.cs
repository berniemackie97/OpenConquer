using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace OpenConquer.Protocol.Crypto
{
    public class BlowfishCfb64Cipher
    {
        private KeyParameter _keyParam = null!;
        private IBufferedCipher _encryptCipher = null!;
        private IBufferedCipher _decryptCipher = null!;

        public void SetKey(byte[] key)
        {
            _keyParam = ParameterUtilities.CreateKeyParameter("Blowfish", key);
        }

        public void SetIvs(byte[] encryptIv, byte[] decryptIv)
        {
            _encryptCipher = CipherUtilities.GetCipher("Blowfish/CFB/NoPadding");
            _encryptCipher.Init(true, new ParametersWithIV(_keyParam, encryptIv));

            _decryptCipher = CipherUtilities.GetCipher("Blowfish/CFB/NoPadding");
            _decryptCipher.Init(false, new ParametersWithIV(_keyParam, decryptIv));
        }

        public void Encrypt(byte[] packet)
        {
            if (_encryptCipher == null)
            {
                throw new InvalidOperationException("Ivs must be set before encrypting.");
            }

            byte[] result = _encryptCipher.DoFinal(packet);
            Buffer.BlockCopy(result, 0, packet, 0, result.Length);
        }

        public void Decrypt(byte[] packet)
        {
            if (_decryptCipher == null)
            {
                throw new InvalidOperationException("Ivs must be set before decrypting.");
            }

            byte[] result = _decryptCipher.DoFinal(packet);
            Buffer.BlockCopy(result, 0, packet, 0, result.Length);
        }
    }
}
