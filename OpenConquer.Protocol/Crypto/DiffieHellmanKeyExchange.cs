using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace OpenConquer.Protocol.Crypto
{
    public class DiffieHellmanKeyExchange
    {
        private const int PadLen = 11;
        private const int JunkLen = 12;
        private const string TqServerTag = "TQServer";

        private readonly string _pHex = "E7A69EBDF105F2A6BBDEAD7E798F76A209AD73FB466431E2E7352ED262F8C558F10BEFEA977DE9E21DCEE9B04D245F300ECCBBA03E72630556D011023F9E857F";
        private readonly string _gHex = "05";

        private readonly DHParameters _dhParams;
        private readonly DHPrivateKeyParameters _privKey;
        private readonly DHPublicKeyParameters _pubKey;
        private readonly byte[] _serverIv = new byte[8];
        private readonly byte[] _clientIv = new byte[8];

        public DiffieHellmanKeyExchange()
        {
            RandomNumberGenerator.Fill(_serverIv);
            RandomNumberGenerator.Fill(_clientIv);

            Org.BouncyCastle.Math.BigInteger p = new(_pHex, 16);
            Org.BouncyCastle.Math.BigInteger g = new(_gHex, 16);
            _dhParams = new DHParameters(p, g);

            DHBasicKeyPairGenerator keyGen = new();
            keyGen.Init(new DHKeyGenerationParameters(new SecureRandom(), _dhParams));
            AsymmetricCipherKeyPair pair = keyGen.GenerateKeyPair();
            _privKey = (DHPrivateKeyParameters)pair.Private;
            _pubKey = (DHPublicKeyParameters)pair.Public;
        }

        public byte[] CreateServerKeyPacket()
        {
            // ascii bytes of hex‐strings
            byte[] pBytes = System.Text.Encoding.ASCII.GetBytes(_pHex);
            byte[] gBytes = System.Text.Encoding.ASCII.GetBytes(_gHex);
            string pubHex = _pubKey.Y.ToString(16).ToUpperInvariant();
            byte[] pubBytes = System.Text.Encoding.ASCII.GetBytes(pubHex);
            byte[] tqBytes = System.Text.Encoding.ASCII.GetBytes(TqServerTag);

            int fullSize = PadLen
                            + 4   // length field
                            + 4   // junkLen
                            + JunkLen
                            + 4   // clientIvLen
                            + _clientIv.Length
                            + 4   // serverIvLen
                            + _serverIv.Length
                            + 4   // pLen
                            + pBytes.Length
                            + 4   // gLen
                            + gBytes.Length
                            + 4   // pubLen
                            + pubBytes.Length
                            + tqBytes.Length;

            uint remainingLength = (uint)(fullSize - PadLen);

            using MemoryStream ms = new(fullSize);
            // pad
            byte[] pad = new byte[PadLen];
            RandomNumberGenerator.Fill(pad);
            ms.Write(pad, 0, pad.Length);
            // length
            ms.Write(BitConverter.GetBytes(remainingLength), 0, 4);
            // junkLen + junk
            ms.Write(BitConverter.GetBytes((uint)JunkLen), 0, 4);
            byte[] junk = new byte[JunkLen];
            RandomNumberGenerator.Fill(junk);
            ms.Write(junk, 0, junk.Length);
            // client IV
            ms.Write(BitConverter.GetBytes((uint)_clientIv.Length), 0, 4);
            ms.Write(_clientIv, 0, _clientIv.Length);
            // server IV
            ms.Write(BitConverter.GetBytes((uint)_serverIv.Length), 0, 4);
            ms.Write(_serverIv, 0, _serverIv.Length);
            // P
            ms.Write(BitConverter.GetBytes((uint)pBytes.Length), 0, 4);
            ms.Write(pBytes, 0, pBytes.Length);
            // G
            ms.Write(BitConverter.GetBytes((uint)gBytes.Length), 0, 4);
            ms.Write(gBytes, 0, gBytes.Length);
            // Server public key
            ms.Write(BitConverter.GetBytes((uint)pubBytes.Length), 0, 4);
            ms.Write(pubBytes, 0, pubBytes.Length);
            // TQServer
            ms.Write(tqBytes, 0, tqBytes.Length);

            return ms.ToArray();
        }

        public BlowfishCfb64Cipher HandleClientKeyPacket(string clientPubHex, BlowfishCfb64Cipher crypto)
        {
            Org.BouncyCastle.Math.BigInteger clientY = new(clientPubHex, 16);
            DHPublicKeyParameters clientParams = new(clientY, _dhParams);

            DHBasicAgreement agree = new();
            agree.Init(_privKey);
            Org.BouncyCastle.Math.BigInteger secretBI = agree.CalculateAgreement(clientParams);
            byte[] secret = secretBI.ToByteArrayUnsigned();

            crypto.SetKey(secret);
            crypto.SetIvs(_clientIv, _serverIv);
            return crypto;
        }
    }
}
