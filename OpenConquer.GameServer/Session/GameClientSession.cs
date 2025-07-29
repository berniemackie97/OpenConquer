using System.Net.Sockets;
using System.Text;
using OpenConquer.GameServer.Crypto;

namespace OpenConquer.GameServer.Session
{
    /// <summary>
    /// Handles the Conquer game‑port handshake and initial packet exchange.
    /// </summary>
    public class GameClientSession(TcpClient tcpClient, ILogger<GameClientSession> logger)
    {
        private readonly TcpClient _tcpClient = tcpClient;
        private readonly ILogger<GameClientSession> _logger = logger;

        // from Albetros.Common.GameKey
        private static readonly byte[] StaticKey = Encoding.ASCII.GetBytes("BC234xs45nme7HU9");

        // handshake constants
        private const int PadLen = 11;
        private const int JunkLen = 12;
        private static readonly byte[] ZeroIv = new byte[8];

        public async Task HandleGameHandshakeAsync(CancellationToken ct)
        {
            System.Net.EndPoint? endpoint = _tcpClient.Client.RemoteEndPoint;
            _logger.LogInformation("Game client connected from {Endpoint}", endpoint);

            using NetworkStream stream = _tcpClient.GetStream();
            _tcpClient.Client.NoDelay = true;

            //
            // ─── Step A: send server DH key packet ───
            //
            DiffieHellmanKeyExchange dh = new();
            byte[] serverBytes = dh.CreateServerKeyPacket();
            _logger.LogInformation("Sending server DH key packet ({Length} bytes)", serverBytes.Length);
            await stream.WriteAsync(serverBytes, ct);

            //
            // ─── Step B: receive & decrypt client key blob ───
            //
            BlowfishCfb64Cipher liveCipher = new();
            liveCipher.SetKey(StaticKey);
            liveCipher.SetIvs(ZeroIv, ZeroIv);

            // 1) peek total length
            int totalLen = await PeekEncryptedLengthAsync(stream, PadLen, ct);

            // 2) read & decrypt the entire DH blob
            byte[] dhBlob = await ReadAndDecryptAsync(stream, liveCipher, totalLen, ct);

            // 3) extract client DH public‑key hex
            string clientPubHex = ParseClientDhPublicKey(dhBlob);
            _logger.LogInformation("Received client DH public key (hex, {Length} chars)", clientPubHex.Length);

            // 4) switch to DH‑derived Blowfish
            liveCipher = dh.HandleClientKeyPacket(clientPubHex, liveCipher);
            _logger.LogInformation("DH handshake complete; switched to shared‑secret cipher");

            //
            // ─── Steps C–F ─── …next: login header/body, parse GameLoginRequestPacket,
            // build & encrypt your WorldListPacket, etc.
            //
        }

        /// <summary>
        /// Reads PadLen+4 bytes, temporarily decrypts them to get the 'remaining' length,
        /// and returns the total packet length (pad + remaining).
        /// </summary>
        private static async Task<int> PeekEncryptedLengthAsync(NetworkStream stream, int padLen, CancellationToken ct)
        {
            byte[] headerEnc = new byte[padLen + 4];
            if (!await ReadExactAsync(stream, headerEnc, headerEnc.Length, ct))
            {
                throw new IOException("Failed reading encrypted header");
            }

            // throw‐away cipher so we don't advance the real cipher’s IVs
            BlowfishCfb64Cipher peekCipher = new();
            peekCipher.SetKey(StaticKey);
            peekCipher.SetIvs(ZeroIv, ZeroIv);
            peekCipher.Decrypt(headerEnc);

            uint remaining = BitConverter.ToUInt32(headerEnc, padLen);
            return (int)(padLen + remaining);
        }

        /// <summary>
        /// Reads exactly 'totalLen' bytes from the stream and decrypts them in one shot.
        /// </summary>
        private static async Task<byte[]> ReadAndDecryptAsync(NetworkStream stream, BlowfishCfb64Cipher cipher, int totalLen, CancellationToken ct)
        {
            byte[] fullEnc = new byte[totalLen];
            if (!await ReadExactAsync(stream, fullEnc, totalLen, ct))
            {
                throw new IOException("Failed reading full encrypted blob");
            }

            cipher.Decrypt(fullEnc);
            return fullEnc;
        }

        /// <summary>
        /// Parses out the ASCII client‑public‑key hex from a decrypted DH blob.
        /// </summary>
        private static string ParseClientDhPublicKey(byte[] blob)
        {
            int pos = PadLen;
            pos += 4; // skip remaining‑length
            int junkLen = (int)BitConverter.ToUInt32(blob, pos);
            pos += 4 + junkLen;
            int ciLen = (int)BitConverter.ToUInt32(blob, pos);
            pos += 4 + ciLen;
            int siLen = (int)BitConverter.ToUInt32(blob, pos);
            pos += 4 + siLen;
            int pLen = (int)BitConverter.ToUInt32(blob, pos);
            pos += 4 + pLen;
            int gLen = (int)BitConverter.ToUInt32(blob, pos);
            pos += 4 + gLen;
            int pubLen = (int)BitConverter.ToUInt32(blob, pos);
            pos += 4;

            return Encoding.ASCII.GetString(blob, pos, pubLen);
        }

        private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
                if (read == 0)
                {
                    return false;
                }

                offset += read;
            }
            return true;
        }
    }
}
