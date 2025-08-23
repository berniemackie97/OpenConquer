using System;
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
    public class LoginClientSession : IAsyncDisposable
    {
        private const int HeaderSize = 4;
        private const int MinPacketSize = 4;
        private const int MaxPacketSize = 1024;

        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly LoginCipher _cipher;
        private readonly IAccountService _accounts;
        private readonly ILoginKeyProvider _keyProvider;
        private readonly ILogger<LoginClientSession> _logger;
        private readonly int _gamePort;
        private readonly string _externalIp;

        public LoginClientSession(TcpClient tcpClient, IAccountService accounts, ILoginKeyProvider keyProvider, ILogger<LoginClientSession> logger, IOptions<NetworkSettings> settings)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _stream = _tcpClient.GetStream();
            _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gamePort = settings?.Value.GamePort ?? throw new ArgumentNullException(nameof(settings));
            _externalIp = settings.Value.ExternalIp;
            _cipher = new LoginCipher();
        }

        public async Task HandleHandshakeAsync(CancellationToken ct)
        {
            System.Net.EndPoint? endpoint = _tcpClient.Client.RemoteEndPoint;
            _logger.LogInformation("Starting handshake for {Endpoint}", endpoint);

            try
            {
                uint seed = await SendSeedAsync(ct);
                (ushort pktLen, ushort pktId, byte[] fullPacket) = await ReadAndDecryptRequestAsync(ct);

                _logger.LogInformation("Received login request (Len={Len} Id={Id})", pktLen, pktId);
                LoginRequestPacket req = LoginRequestPacket.Parse(fullPacket);
                _logger.LogInformation("Parsed LoginRequest for {User}", req.Username);

                await RespondAsync(req, seed, ct).ConfigureAwait(false);

                _logger.LogInformation("Handshake complete, closed login session for {Endpoint}", endpoint);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Handshake canceled for {Endpoint}", endpoint);
                Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handshake failed for {Endpoint}", endpoint);
                Disconnect();
            }
        }

        private async Task<uint> SendSeedAsync(CancellationToken ct)
        {
            const int MinSeed = 100_000, MaxSeed = 90_000_000;
            uint seed = (uint)RandomNumberGenerator.GetInt32(MinSeed, MaxSeed);
            SeedResponsePacket packet = new(seed);
            byte[] data = PacketWriter.Serialize(packet);

            await SendToClientAsync(data, ct).ConfigureAwait(false);
            _logger.LogInformation("SeedResponsePacket sent (Seed={Seed})", seed);
            return seed;
        }

        private async Task<(ushort Len, ushort Id, byte[] Full)> ReadAndDecryptRequestAsync(CancellationToken ct)
        {
            byte[] headerEnc = new byte[HeaderSize];
            if (!await ReadExactAsync(headerEnc, ct))
            {
                throw new IOException("Failed to read login request header");
            }

            _cipher.Decrypt(headerEnc, HeaderSize);
            ushort len = BitConverter.ToUInt16(headerEnc, 0);
            ushort id = BitConverter.ToUInt16(headerEnc, 2);

            if (len < MinPacketSize || len > MaxPacketSize)
            {
                throw new InvalidDataException($"Invalid packet length {len}");
            }

            int bodyLen = len - HeaderSize;
            byte[] bodyEnc = new byte[bodyLen];
            if (!await ReadExactAsync(bodyEnc, ct))
            {
                throw new IOException("Failed to read login request body");
            }

            _cipher.Decrypt(bodyEnc, bodyLen);
            byte[] full = new byte[len];
            Buffer.BlockCopy(headerEnc, 0, full, 0, HeaderSize);
            Buffer.BlockCopy(bodyEnc, 0, full, HeaderSize, bodyLen);
            return (len, id, full);
        }

        private async Task RespondAsync(LoginRequestPacket req, uint seed, CancellationToken ct)
        {
            string password = DecryptPassword(req, seed);
            AuthResponsePacket resp = await BuildResponseAsync(req.Username, password, ct).ConfigureAwait(false);

            _logger.LogInformation("AuthResponse: Port={Port}, ExternalIp='{IP}'", resp.Port, resp.ExternalIp);
            byte[] outBuf = PacketWriter.Serialize(resp);
            _logger.LogInformation("AuthResponsePacket bytes: {Hex}", BitConverter.ToString(outBuf));
            await SendToClientAsync(outBuf, ct).ConfigureAwait(false);
            _logger.LogInformation("Sent AuthResponse (Key={Key}) for {User}", resp.Key, req.Username);
        }

        private async Task SendToClientAsync(byte[] buffer, CancellationToken ct)
        {
            _cipher.Encrypt(buffer, buffer.Length);
            await _stream.WriteAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
            await _stream.FlushAsync(ct).ConfigureAwait(false);
        }

        private void Disconnect()
        {
            try
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
            }
            catch { /* ignore */ }
        }

        private static string DecryptPassword(LoginRequestPacket req, uint seed)
        {
            var rc5 = new RC5Cipher(seed);

            byte[] decrypted = new byte[req.PasswordBlob.Length];
            rc5.Decrypt(req.PasswordBlob, decrypted);

            byte[] plain = new ConquerPasswordCryptographer(req.Username).Decrypt(decrypted, decrypted.Length);

            string pass = Encoding.ASCII.GetString(plain).TrimEnd('\0');

            var sb = new StringBuilder(pass.Length);
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

        private async Task<AuthResponsePacket> BuildResponseAsync(string user, string pass, CancellationToken ct)
        {
            uint loginSessionKey = _keyProvider.NextKey();
            uint accountSessionHash = (uint)Random.Shared.Next(1, 1000000);

            Account? acct = await _accounts.GetByUsernameAsync(user).ConfigureAwait(false);

            if (acct is null)
            {
                acct = new Account
                {
                    Username = user,
                    Password = pass,
                    Permission = PlayerPermission.Player,
                    Hash = accountSessionHash,
                    Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                acct = await _accounts.CreateAsync(acct, ct).ConfigureAwait(false);

                if (acct is null || acct.UID == 0)
                {
                    return AuthResponsePacket.CreateInvalid();
                }
            }
            else if (acct.Password != pass || acct.Permission == PlayerPermission.Error)
            {
                return AuthResponsePacket.CreateInvalid();
            }
            else if (acct.Permission == PlayerPermission.Banned)
            {
                return new AuthResponsePacket
                {
                    Key = AuthResponsePacket.RESPONSE_BANNED
                };
            }
            else
            {
                acct.Hash = accountSessionHash;
                await _accounts.UpdateHashAsync(acct.UID, accountSessionHash, ct).ConfigureAwait(false);
            }

            return new AuthResponsePacket
            {
                UID = loginSessionKey,
                Key = acct.UID,
                Port = (uint)_gamePort,
                ExternalIp = _externalIp
            };
        }

        private async Task<bool> ReadExactAsync(byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int n = await _stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct).ConfigureAwait(false);
                if (n == 0)
                {
                    return false;
                }

                offset += n;
            }
            return true;
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            Disconnect();
            return ValueTask.CompletedTask;
        }
    }
}
