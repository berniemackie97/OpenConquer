using System.Net.Sockets;
using System.Text;
using OpenConquer.Domain.Entities;
using OpenConquer.GameServer.Dispatchers;
using OpenConquer.GameServer.Session.Managers;
using OpenConquer.Protocol.Crypto;
using OpenConquer.Protocol.Packets;
using OpenConquer.Protocol.Packets.Parsers;

namespace OpenConquer.GameServer.Session
{
    public class GameClientSession(TcpClient tcpClient, ILogger<GameClientSession> logger, PacketParserRegistry parser, PacketDispatcher dispatcher, WorldManager worldManager)
    {
        private readonly TcpClient _tcpClient = tcpClient;
        private readonly ILogger<GameClientSession> _logger = logger;
        private readonly PacketParserRegistry _parser = parser;
        private readonly PacketDispatcher _dispatcher = dispatcher;

        private NetworkStream? _stream;
        private BlowfishCfb64Cipher? _cipher;

        private static readonly byte[] StaticKey = Encoding.ASCII.GetBytes("BC234xs45nme7HU9");
        private const int PadLen = 11;
        private static readonly byte[] ZeroIv = new byte[8];

        public Character? Character { get; set; }

        public WorldManager World { get; } = worldManager;

        public Character User => Character ?? throw new InvalidOperationException("Character not yet loaded.");

        public Task DisconnectAsync(CancellationToken ct)
        {
            _logger.LogInformation("Disconnecting client {Endpoint}", _tcpClient.Client.RemoteEndPoint);
            try
            {
                _stream?.Close();
                _tcpClient.Client.Shutdown(SocketShutdown.Both);
            }
            catch { /* swallow */ }
            _tcpClient.Close();
            return Task.CompletedTask;
        }

        public async Task HandleGameHandshakeAsync(CancellationToken ct)
        {
            _stream = _tcpClient.GetStream();
            _tcpClient.Client.NoDelay = true;

            DiffieHellmanKeyExchange dh = new();
            byte[] serverKeyPacket = dh.CreateServerKeyPacket();
            _logger.LogInformation("Sending server DH key packet ({Length} bytes)", serverKeyPacket.Length);
            await _stream.WriteAsync(serverKeyPacket, ct);

            _cipher = new BlowfishCfb64Cipher();
            _cipher.SetKey(StaticKey);
            _cipher.SetIvs(ZeroIv, ZeroIv);

            int totalLen = await PeekEncryptedLengthAsync(_stream, PadLen, ct);
            byte[] dhBlob = await ReadAndDecryptAsync(_stream, _cipher, totalLen, ct);

            string clientPubHex = ParseClientDhPublicKey(dhBlob);
            _logger.LogInformation("Received client DH public key (hex, {Length} chars)", clientPubHex.Length);

            _cipher = dh.HandleClientKeyPacket(clientPubHex, _cipher);
            _logger.LogInformation("DH handshake complete; switched to shared‐secret cipher");

            await ProcessIncomingPacketsAsync(ct);
        }

        private async Task ProcessIncomingPacketsAsync(CancellationToken ct)
        {
            if (_stream is null || _cipher is null)
            {
                throw new InvalidOperationException("Handshake not completed.");
            }

            while (!ct.IsCancellationRequested)
            {
                int totalLen = await PeekEncryptedLengthAsync(_stream, PadLen, ct);
                byte[] encrypted = new byte[totalLen];
                if (!await ReadExactAsync(_stream, encrypted, totalLen, ct))
                {
                    break; // client disconnected
                }

                _cipher.Decrypt(encrypted);
                IPacket packet = _parser.ParsePacket(encrypted);
                await _dispatcher.DispatchAsync(packet, this, ct);
            }
        }

        public Task SendAsync<TPacket>(TPacket packet, CancellationToken ct)
        {
            if (_stream is null || _cipher is null)
            {
                throw new InvalidOperationException("Cannot send before handshake.");
            }

            byte[] buffer = packet switch
            {
                byte[] b => b,
                _ => (byte[])(object)packet!
            };

            _cipher.Encrypt(buffer);
            return _stream.WriteAsync(buffer.AsMemory(0, buffer.Length), ct).AsTask();
        }

        private static async Task<int> PeekEncryptedLengthAsync(NetworkStream stream, int padLen, CancellationToken ct)
        {
            byte[] headerEnc = new byte[padLen + 4];
            if (!await ReadExactAsync(stream, headerEnc, headerEnc.Length, ct))
            {
                throw new IOException("Failed reading encrypted header");
            }

            BlowfishCfb64Cipher peek = new();
            peek.SetKey(StaticKey);
            peek.SetIvs(ZeroIv, ZeroIv);
            peek.Decrypt(headerEnc);

            uint remaining = BitConverter.ToUInt32(headerEnc, padLen);
            return padLen + (int)remaining;
        }

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

        private static string ParseClientDhPublicKey(byte[] blob)
        {
            int pos = PadLen;
            pos += 4;
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
