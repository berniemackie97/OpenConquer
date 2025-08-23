using System;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace OpenConquer.Protocol.Crypto
{
    public static class DiffieHellmanHandshake
    {
        public class KeyExchangeSession
        {
            // Replaces OpenSSL.DH
            private DHParameters _dhParams;
            private AsymmetricCipherKeyPair _keyPair;

            private byte[] _serverIv;
            private byte[] _clientIv;

            public byte[] CreateServerKeyPacket()
            {
                _clientIv = new byte[8];
                _serverIv = new byte[8];

                string P = "E7A69EBDF105F2A6BBDEAD7E798F76A209AD73FB466431E2E7352ED262F8C558F10BEFEA977DE9E21DCEE9B04D245F300ECCBBA03E72630556D011023F9E857F";
                string G = "05";

                var p = new BigInteger(P, 16);
                var g = new BigInteger(G, 16);
                _dhParams = new DHParameters(p, g);

                var gen = new DHKeyPairGenerator();
                gen.Init(new DHKeyGenerationParameters(new SecureRandom(), _dhParams));
                _keyPair = gen.GenerateKeyPair();

                // OpenSSL BN_bn2hex style: uppercase, no leading zeros
                var y = ((DHPublicKeyParameters)_keyPair.Public).Y;
                string serverPublicHex = ToOpenSslHex(y);

                return GeneratePacket(_serverIv, _clientIv, P, G, serverPublicHex);
            }

            public CastCfb64Cipher HandleClientKeyPacket(string PublicKey, CastCfb64Cipher cryptographer)
            {
                var clientY = new BigInteger(PublicKey, 16);
                var clientPub = new DHPublicKeyParameters(clientY, _dhParams);

                var agree = new DHBasicAgreement();
                agree.Init((DHPrivateKeyParameters)_keyPair.Private);
                var z = agree.CalculateAgreement(clientPub); // BigInteger

                // OpenSSL DH_compute_key output: big-endian, length = DH_size(p)
                var secret = ToOpenSslDhSecret(z, _dhParams.P.BitLength);

                // Mirror OpenSSL CAST_set_key behavior: use at most 16 bytes
                var castKey = ToCastKey16(secret);

                cryptographer.SetKey(castKey);
                cryptographer.SetIvs(_clientIv, _serverIv);
                return cryptographer;
            }

            public byte[] GeneratePacket(byte[] ServerIV1, byte[] ServerIV2, string P, string G, string ServerPublicKey)
            {
                const int PAD_LEN = 11;
                const int junkLen = 12;
                const string tqs = "TQServer";

                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);

                var pad = new byte[PAD_LEN];
                RandomNumberGenerator.Fill(pad);
                var junk = new byte[junkLen];
                RandomNumberGenerator.Fill(junk);

                int size = 47 + P.Length + G.Length + ServerPublicKey.Length + 12 + 8 + 8;

                bw.Write(pad);
                bw.Write(size - PAD_LEN);
                bw.Write((UInt32)junkLen);
                bw.Write(junk);
                bw.Write((UInt32)ServerIV2.Length);
                bw.Write(ServerIV2);
                bw.Write((UInt32)ServerIV1.Length);
                bw.Write(ServerIV1);
                bw.Write((UInt32)P.Length);
                foreach (char c in P) bw.BaseStream.WriteByte((byte)c);
                bw.Write((UInt32)G.Length);
                foreach (char c in G) bw.BaseStream.WriteByte((byte)c);
                bw.Write((UInt32)ServerPublicKey.Length);
                foreach (char c in ServerPublicKey) bw.BaseStream.WriteByte((byte)c);
                foreach (char c in tqs) bw.BaseStream.WriteByte((byte)c);

                return ms.ToArray();
            }

            // ---- OpenSSL parity helpers ----

            // Uppercase hex, no leading zeros; "0" for zero.
            private static string ToOpenSslHex(BigInteger x)
            {
                string s = x.ToString(16);
                s = s.TrimStart('0');
                return s.Length == 0 ? "0" : s.ToUpperInvariant();
            }

            // DH_compute_key => DH_size(p) bytes, big-endian; left-pad with zeros
            private static byte[] ToOpenSslDhSecret(BigInteger z, int pBitLength)
            {
                var raw = z.ToByteArrayUnsigned();
                int targetLen = (pBitLength + 7) / 8;
                if (raw.Length == targetLen) return raw;

                var outBuf = new byte[targetLen];
                Buffer.BlockCopy(raw, 0, outBuf, targetLen - raw.Length, raw.Length);
                return outBuf;
            }

            // OpenSSL CAST_set_key clamps key length to 16; use the first 16 bytes provided.
            private static byte[] ToCastKey16(byte[] dhSecret)
            {
                if (dhSecret.Length <= 16)
                {
                    // BC accepts 5..16; in practice DH_size here >> 16
                    var copy = new byte[dhSecret.Length];
                    Buffer.BlockCopy(dhSecret, 0, copy, 0, dhSecret.Length);
                    return copy;
                }

                var key = new byte[16];
                Buffer.BlockCopy(dhSecret, 0, key, 0, 16); // take the leading (MSB-side) 16 bytes
                return key;
            }
        }
    }
}
