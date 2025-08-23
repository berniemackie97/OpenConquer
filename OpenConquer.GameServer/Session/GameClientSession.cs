using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.GameServer.Dispatchers;
using OpenConquer.GameServer.Session.Managers;
using OpenConquer.Protocol.Crypto;
using OpenConquer.Protocol.Packets;
using OpenConquer.Protocol.Packets.Parsers;

namespace OpenConquer.GameServer.Session
{
    public class GameClientSession(
        ConnectionContext context,
        ILogger<GameClientSession> logger,
        PacketParserRegistry parser,
        PacketDispatcher dispatcher,
        WorldManager worldManager,
        UserManager userManager,
        ICharacterService characterService) : IAsyncDisposable
    {
        private readonly ConnectionContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<GameClientSession> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly PacketParserRegistry _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        private readonly PacketDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        private readonly ICharacterService _characterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
        private readonly WorldManager _worldManager = worldManager ?? throw new ArgumentNullException(nameof(worldManager));
        private readonly UserManager _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

        private static readonly byte[] StaticKey = Encoding.ASCII.GetBytes("BC234xs45nme7HU9");
        private const string TqServerTag = "TQServer";
        private const int TagLength = 8;

        private const int ClientPadLen = 7;

        private CastCfb64Cipher _cipher = new(StaticKey); // static CAST key, zero IVs for exchange
        private DiffieHellmanHandshake.KeyExchangeSession _dh = default!;

        private bool _isHandshaking = true;

        public Character? Character { get; set; }
        public Character User => Character ?? throw new InvalidOperationException("Character not yet loaded.");
        public WorldManager World => _worldManager;

        public async Task HandleGameHandshakeAsync(CancellationToken ct)
        {
            _context.TcpClient.Client.NoDelay = true;
            _logger.LogInformation("Starting game handshake.");
            _isHandshaking = true;

            _context.Cipher = _cipher;

            // 1) Server DH packet (already includes "TQServer")
            _dh = new DiffieHellmanHandshake.KeyExchangeSession();
            byte[] serverDhPacket = _dh.CreateServerKeyPacket();

            // 2) Send it encrypted with the static key (clone so we don't mutate the original buffer)
            {
                var toSend = (byte[])serverDhPacket.Clone();
                _cipher.Encrypt(toSend);
                await _context.Stream.WriteAsync(toSend, 0, toSend.Length, ct).ConfigureAwait(false);
                await _context.Stream.FlushAsync(ct).ConfigureAwait(false);
            }

            // 3) Read client's DH response (encrypted with static key)
            byte[] fullPlain = await ReadClientDhResponseAsync(_context.Stream, ct).ConfigureAwait(false);

            // 4) Extract client public key
            string clientPubHex = ParseClientDhPublicKey(fullPlain);
            _logger.LogInformation("Received client DH public key (len={Length})", clientPubHex.Length);

            // 5) Re-key CAST using DH secret; IVs set to enc=clientIV / dec=serverIV inside HandleClientKeyPacket
            _cipher = _dh.HandleClientKeyPacket(clientPubHex, _cipher);
            _context.Cipher = _cipher;

            _isHandshaking = false;
            _logger.LogInformation("DH handshake complete.");

            await ProcessIncomingPacketsAsync(ct);
        }

        private async Task ProcessIncomingPacketsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                byte[]? frame = await ReadOneGamePacketAsync(_context.Stream, ct);
                if (frame is null) break;

                IPacket packet = _parser.ParsePacket(frame);
                await _dispatcher.DispatchAsync(packet, this, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Wire format (post-handshake): [enc]{ len(2) + body(len-2) + "TQServer"(8) }
        /// Returns the decrypted packet without the trailing tag.
        /// </summary>
        private async Task<byte[]?> ReadOneGamePacketAsync(NetworkStream stream, CancellationToken ct)
        {
            // Read + decrypt 2-byte length
            byte[] lenBuf = new byte[2];
            if (!await ReadExactAsync(stream, lenBuf, 2, ct)) return null;
            _cipher.Decrypt(lenBuf);
            ushort len = BinaryPrimitives.ReadUInt16LittleEndian(lenBuf);
            if (len < 12 || len > 1024)
                throw new IOException($"Invalid packet length {len}");

            // Read + decrypt body(len-2) + tag(8)
            int restToRead = (len - 2) + TagLength;
            byte[] restBuf = new byte[restToRead];
            if (!await ReadExactAsync(stream, restBuf, restToRead, ct)) return null;
            _cipher.Decrypt(restBuf);

            // (Optional) validate trailing tag
            // var tagOk = restBuf.AsSpan(len - 2, TagLength).SequenceEqual(Encoding.ASCII.GetBytes(TqServerTag));

            // Stitch header + body (strip tag)
            byte[] full = new byte[len];
            Buffer.BlockCopy(lenBuf, 0, full, 0, 2);
            Buffer.BlockCopy(restBuf, 0, full, 2, len - 2);
            return full;
        }

        /// <summary>
        /// Client DH response (plaintext after decrypt):
        /// [7 pad][4 PacketLen][4 JunkLen][Junk][4 PubLen][PubHex]...
        /// PacketLen excludes the initial 7 pad bytes.
        /// </summary>
        private async Task<byte[]> ReadClientDhResponseAsync(NetworkStream stream, CancellationToken ct)
        {
            int head = ClientPadLen + 4; // 7 + sizeof(PacketLen)
            byte[] headBuf = new byte[head];
            if (!await ReadExactAsync(stream, headBuf, head, ct))
                throw new IOException("Disconnected while reading DH response header.");

            _cipher.Decrypt(headBuf);
            uint packetLen = BinaryPrimitives.ReadUInt32LittleEndian(headBuf.AsSpan(ClientPadLen, 4));
            if (packetLen < 12 || packetLen > 4096)
                throw new IOException($"Suspicious DH response length {packetLen}");

            int total = ClientPadLen + (int)packetLen;
            byte[] plain = new byte[total];
            Buffer.BlockCopy(headBuf, 0, plain, 0, headBuf.Length);

            int remaining = total - headBuf.Length;
            int offset = headBuf.Length;

            while (remaining > 0)
            {
                int chunk = Math.Min(remaining, 2048);
                byte[] buf = new byte[chunk];
                if (!await ReadExactAsync(stream, buf, chunk, ct))
                    throw new IOException("Disconnected while reading DH response body.");

                _cipher.Decrypt(buf); // in-place
                Buffer.BlockCopy(buf, 0, plain, offset, chunk);
                offset += chunk;
                remaining -= chunk;
            }

            return plain;
        }

        private static string ParseClientDhPublicKey(ReadOnlySpan<byte> fullPlain)
        {
            int pos = ClientPadLen; // 7
            uint packetLen = BinaryPrimitives.ReadUInt32LittleEndian(fullPlain.Slice(pos, 4));
            pos += 4;

            int junkLen = BinaryPrimitives.ReadInt32LittleEndian(fullPlain.Slice(pos, 4));
            pos += 4 + junkLen;

            int pubLen = BinaryPrimitives.ReadInt32LittleEndian(fullPlain.Slice(pos, 4));
            pos += 4;

            if (pubLen <= 0 || pos + pubLen > fullPlain.Length)
                throw new InvalidDataException("Invalid DH pubkey length in client response.");

            return Encoding.ASCII.GetString(fullPlain.Slice(pos, pubLen));
        }

        public async Task SendAsync(byte[] buffer, CancellationToken ct)
        {
            if (_isHandshaking)
            {
                // Exchange phase: CreateServerKeyPacket already has "TQServer".
                var toSend = (byte[])buffer.Clone();
                _cipher.Encrypt(toSend);
                await _context.Stream.WriteAsync(toSend, 0, toSend.Length, ct).ConfigureAwait(false);
                await _context.Stream.FlushAsync(ct).ConfigureAwait(false);
                return;
            }

            // Normal packets: stamp "TQServer" and hand to writer (which encrypts using _context.Cipher)
            int offset = buffer.Length - TagLength;
            Encoding.ASCII.GetBytes(TqServerTag, 0, TagLength, buffer, offset);

            await _context.SendPacketAsync(buffer, ct).ConfigureAwait(false);
        }

        public async Task SendAsync<TPacket>(TPacket packet, CancellationToken ct)
            where TPacket : IPacket
        {
            byte[] buffer = PacketWriter.Serialize(packet);
            await SendAsync(buffer, ct).ConfigureAwait(false);
        }

        public async Task DisconnectAsync(CancellationToken ct)
        {
            _logger.LogInformation("Disconnecting game client {Endpoint}", _context.TcpClient.Client.RemoteEndPoint);

            if (Character != null)
            {
                try
                {
                    await _characterService.SaveAsync(Character, ct).ConfigureAwait(false);
                    _logger.LogInformation("Saved character UID={UID}", Character.UID);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving character UID={UID}", Character.UID);
                }
                _worldManager.RemovePlayer(Character.UID);
                _userManager.Logout(Character.UID);
            }

            await _context.DisconnectAsync().ConfigureAwait(false);
        }

        private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
        {
            int offset = 0;
            while (offset < count)
            {
                int n = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct).ConfigureAwait(false);
                if (n == 0) return false;
                offset += n;
            }
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            await _context.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
