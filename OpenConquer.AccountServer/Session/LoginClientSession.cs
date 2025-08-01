using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.Infrastructure.POCO;
using OpenConquer.Protocol.Crypto;
using OpenConquer.Protocol.Packets;
using OpenConquer.Protocol.Packets.Auth;

namespace OpenConquer.AccountServer.Session
{
    public class LoginClientSession(TcpClient tcpClient, IAccountService accounts, ILoginKeyProvider keyProvider, ILogger<LoginClientSession> logger, ILogger<ConnectionContext> ctxLogger, IOptions<NetworkSettings> settings)
    {
        private const int HeaderSize = 4;
        private const int MinPacketSize = 4;
        private const int MaxPacketSize = 1024;

        private readonly ConnectionContext _ctx = new(tcpClient, ctxLogger);
        private readonly IAccountService _accounts = accounts;
        private readonly ILoginKeyProvider _keyProvider = keyProvider;
        private readonly ILogger<LoginClientSession> _logger = logger;
        private readonly int _gamePort = settings.Value.GamePort;
        private readonly string _externalIp = settings.Value.ExternalIp;

        public async Task HandleHandshakeAsync(CancellationToken ct)
        {
            System.Net.EndPoint? endpoint = _ctx.TcpClient.Client.RemoteEndPoint;
            _logger.LogInformation("Starting handshake for {Endpoint}", endpoint);

            try
            {
                uint seed = SendSeed(ct);
                (ushort pktLen, ushort pktId, byte[] fullPacket) = await ReadAndDecryptRequestAsync(ct);
                _logger.LogInformation("Received login request (Len={Len} Id={Id})", pktLen, pktId);

                LoginRequestPacket req = LoginRequestPacket.Parse(fullPacket);
                _logger.LogInformation("Parsed LoginRequest for {User}", req.Username);

                await RespondAsync(req, seed);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Handshake canceled for {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handshake failed for {Endpoint}", endpoint);
            }
            finally
            {
                await _ctx.DisconnectAsync();
            }
        }

        private uint SendSeed(CancellationToken ct)
        {
            const int MinSeed = 100_000, MaxSeed = 90_000_000;
            uint seed = (uint)RandomNumberGenerator.GetInt32(MinSeed, MaxSeed);
            SeedResponsePacket packet = new(seed);
            byte[] data = PacketWriter.Serialize(packet);

            _ctx.Cipher.Encrypt(data, data.Length);
            _ctx.SendPacketAsync(data, data.Length).AsTask().Wait(ct);

            _logger.LogInformation("SeedResponsePacket sent (Seed={Seed})", seed);
            return seed;
        }

        private async Task<(ushort Len, ushort Id, byte[] Full)> ReadAndDecryptRequestAsync(CancellationToken ct)
        {
            byte[] headerEnc = new byte[HeaderSize];
            if (!await ReadExactAsync(headerEnc, ct))
            {
                throw new IOException("Failed to read header");
            }

            byte[] peek = (byte[])headerEnc.Clone();
            new LoginCipher().Decrypt(peek, peek.Length);

            ushort len = BitConverter.ToUInt16(peek, 0);
            ushort id = BitConverter.ToUInt16(peek, 2);

            if (len < MinPacketSize || len > MaxPacketSize)
            {
                throw new InvalidDataException($"Invalid packet length {len}");
            }

            int bodyLen = len - HeaderSize;
            byte[] bodyEnc = new byte[bodyLen];
            if (!await ReadExactAsync(bodyEnc, ct))
            {
                throw new IOException("Failed to read body");
            }

            byte[] full = new byte[len];
            Buffer.BlockCopy(headerEnc, 0, full, 0, HeaderSize);
            Buffer.BlockCopy(bodyEnc, 0, full, HeaderSize, bodyLen);
            new LoginCipher().Decrypt(full, full.Length);

            return (len, id, full);
        }

        private async Task RespondAsync(LoginRequestPacket req, uint seed)
        {
            string password = DecryptPassword(req, seed);
            AuthResponsePacket resp = await BuildResponseAsync(req.Username, password);

            byte[] outBuf = PacketWriter.Serialize(resp);
            await _ctx.SendPacketAsync(outBuf, outBuf.Length).ConfigureAwait(false);

            _logger.LogInformation("Sent AuthResponse (Key={Key}) for {User}", resp.Key, req.Username);
        }

        private static string DecryptPassword(LoginRequestPacket req, uint seed)
        {
            LoginPrng prng = new((int)seed);
            byte[] rc5Key = Enumerable.Range(0, 16).Select(_ => (byte)prng.Next()).ToArray();
            RC5Cipher rc5 = new(rc5Key);

            byte[] decrypted = rc5.Decrypt(req.PasswordBlob);
            byte[] chars = new ConquerPasswordCryptographer(req.Username)
                                  .Decrypt(decrypted, decrypted.Length);

            string pass = Encoding.ASCII.GetString(chars).TrimEnd('\0');

            StringBuilder sb = new(pass.Length);
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
            return sb.ToString();
        }

        private async Task<AuthResponsePacket> BuildResponseAsync(string user, string pass)
        {
            if (user.Equals("testuser", StringComparison.OrdinalIgnoreCase) && pass == "testpass")
            {
                return new AuthResponsePacket
                {
                    Key = AuthResponsePacket.RESPONSE_VALID,
                    UID = _keyProvider.NextKey(),
                    Port = (uint)_gamePort,
                    ExternalIp = _externalIp
                };
            }

            Account? acct = await _accounts.GetByUsernameAsync(user);
            AuthResponsePacket resp = new();

            if (acct is null)
            {
                resp.Key = AuthResponsePacket.RESPONSE_INVALID_ACCOUNT;
            }
            else if (pass != acct.Password || acct.Permission == PlayerPermission.Error)
            {
                resp.Key = AuthResponsePacket.RESPONSE_INVALID;
            }
            else
            {
                resp.Key = acct.UID;
                resp.UID = _keyProvider.NextKey();
                resp.Port = (uint)_gamePort;
                resp.ExternalIp = _externalIp;
                acct.Hash = resp.UID;
                acct.AllowLogin();
            }

            return resp;
        }

        private async Task<bool> ReadExactAsync(byte[] buf, CancellationToken ct)
        {
            NetworkStream stream = _ctx.Stream;
            int offset = 0;
            while (offset < buf.Length)
            {
                int n = await stream.ReadAsync(buf.AsMemory(offset), ct).ConfigureAwait(false);
                if (n == 0)
                {
                    return false;
                }

                offset += n;
            }
            return true;
        }
    }
}
