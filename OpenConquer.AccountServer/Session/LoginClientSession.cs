using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Enums;
using OpenConquer.Protocol.Implementation.Crypto;
using OpenConquer.Protocol.Packets;
using OpenConquer.Protocol.Packets.Auth;

namespace OpenConquer.AccountServer.Session
{
    public class LoginClientSession(
        TcpClient tcpClient,
        IAccountService accountService,
        ILoginKeyProvider keyProvider,
        ILogger<LoginClientSession> logger)
    {
        private readonly ConnectionContext _ctx = new(tcpClient);
        private readonly IAccountService _accountService = accountService;
        private readonly ILoginKeyProvider _keyProvider = keyProvider;
        private readonly ILogger<LoginClientSession> _logger = logger;

        public async Task HandleHandshakeAsync(CancellationToken ct)
        {
            var endpoint = _ctx.TcpClient.Client.RemoteEndPoint;
            _logger.LogInformation("Client connected from {Endpoint}", endpoint);

            // ─── Step 1A: build & send the 8‑byte seed ───
            uint seed = (uint)RandomNumberGenerator.GetInt32(100_000, 90_000_000);
            var seedPacket = new SeedResponsePacket(seed);
            byte[] seedBytes = seedPacket.Serialize(); // 8 bytes

            // encrypt in‑place
            _ctx.Cipher.Encrypt(seedBytes, seedBytes.Length);
            await _ctx.SendAsync(seedBytes, seedBytes.Length, ct);
            _logger.LogInformation("Sent encrypted SeedResponsePacket(Seed={Seed})", seed);

            // ─── Steps 1C–D: read & peek the next encrypted header ───
            byte[] headerEnc = new byte[4];
            if (!await ReadExactAsync(headerEnc, ct))
            {
                _logger.LogWarning("Failed to read handshake header");
                await _ctx.DisconnectAsync();
                return;
            }

            // peek length with throw‑away cipher
            var headerPeek = (byte[])headerEnc.Clone();
            new LoginCipher().Decrypt(headerPeek, headerPeek.Length);

            ushort pktLen = BitConverter.ToUInt16(headerPeek, 0);
            ushort pktId = BitConverter.ToUInt16(headerPeek, 2);
            _logger.LogInformation("Handshake packet length={Length}, id={Id}", pktLen, pktId);

            if (pktLen < 4 || pktLen > 1024)
            {
                _logger.LogWarning("Invalid packet length {Length}", pktLen);
                await _ctx.DisconnectAsync();
                return;
            }

            // ─── Step 1D: read the rest, decrypt full packet ───
            int bodyLen = pktLen - 4;
            byte[] bodyEnc = new byte[bodyLen];
            if (!await ReadExactAsync(bodyEnc, ct))
            {
                _logger.LogWarning("Failed to read handshake body");
                await _ctx.DisconnectAsync();
                return;
            }

            var fullEnc = new byte[pktLen];
            Buffer.BlockCopy(headerEnc, 0, fullEnc, 0, 4);
            Buffer.BlockCopy(bodyEnc, 0, fullEnc, 4, bodyLen);
            new LoginCipher().Decrypt(fullEnc, fullEnc.Length);

            // ─── Step 1E: parse & log the auth request ───
            var req = LoginRequestPacket.Parse(fullEnc);
            _logger.LogInformation(
                "LoginRequest: PacketId={PacketId}, Username={User}",
                req.PacketID, req.Username);

            // ─── Step 1F: handle auth & reply ───
            await ProcessLoginAsync(req, seed, ct);
        }

        private async Task ProcessLoginAsync(
            LoginRequestPacket req,
            uint seed,
            CancellationToken ct)
        {
            // — derive the RC5 key from the same seed we sent —
            byte[] rc5Key = new byte[16];
            var prng = new LoginPrng((int)seed);
            for (int i = 0; i < rc5Key.Length; i++)
                rc5Key[i] = (byte)prng.Next();

            // — decrypt the password blob —
            var crypt = new RC5Cipher(rc5Key);
            byte[] decrypted = crypt.Decrypt(req.PasswordBlob);
            string pass = Encoding.ASCII
                .GetString(new ConquerPasswordCryptographer(req.Username)
                .Decrypt(decrypted, decrypted.Length))
                .TrimEnd('\0');

            // — apply the numpad->digit fix exactly as Albetros does —
            var sb = new StringBuilder();
            foreach (char c in pass)
            {
                sb.Append(c switch
                {
                    '-' => '0',
                    '#' => '1',
                    '(' => '2',
                    '"' => '3',
                    '%' => '4',
                    '\f' => '5',
                    '\'' => '6',
                    '$' => '7',
                    '&' => '8',
                    '!' => '9',
                    _ => c
                });
            }
            pass = sb.ToString();

            // ─── LOCAL TEST USER (skip DB) ───
            if (req.Username.Equals("testuser", StringComparison.OrdinalIgnoreCase)
             && pass == "testpass")
            {
                var resp = new AuthResponsePacket
                {
                    Key = AuthResponsePacket.RESPONSE_VALID,
                    UID = _keyProvider.NextKey(),
                    Port = 5816,
                    ExternalIp = "192.168.1.58"
                };

                var outBuf = PacketWriter.Serialize(resp);
                _ctx.Cipher.Encrypt(outBuf, outBuf.Length);
                await _ctx.SendAsync(outBuf, outBuf.Length, ct);
                return;
            }

            // ─── FALL BACK TO DATABASE LOOKUP ───
            var account = await _accountService.GetByUsernameAsync(req.Username);
            var respDb = new AuthResponsePacket();

            if (account == null)
                respDb.Key = AuthResponsePacket.RESPONSE_INVALID_ACCOUNT;
            else if (pass != account.Password || account.Permission <= PlayerPermission.Error)
                respDb.Key = AuthResponsePacket.RESPONSE_INVALID;
            else
            {
                respDb.Key = account.UID;
                respDb.UID = _keyProvider.NextKey();
                respDb.Port = 5816;
                respDb.ExternalIp = "127.0.0.1";
                account.Hash = respDb.UID;
                account.AllowLogin();
            }

            var outBufDb = PacketWriter.Serialize(respDb);
            _ctx.Cipher.Encrypt(outBufDb, outBufDb.Length);
            await _ctx.SendAsync(outBufDb, outBufDb.Length, ct);
        }

        private async Task<bool> ReadExactAsync(byte[] buf, CancellationToken ct)
        {
            int offset = 0;
            var stream = _ctx.Stream;
            while (offset < buf.Length)
            {
                int n = await stream.ReadAsync(buf.AsMemory(offset), ct);
                if (n == 0) return false;
                offset += n;
            }
            return true;
        }
    }
}
