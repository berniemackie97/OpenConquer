using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OpenConquer.AccountServer.Session;
using OpenConquer.Domain.Contracts;
using OpenConquer.Protocol.Implementation.Crypto;
using OpenConquer.Protocol.Packets.Auth;

namespace OpenConquer.Tests.Sessions
{
    public class LoginClientSessionTests
    {
        #region FixNumpadChars Tests

        [Theory]
        [InlineData("-", "0")]
        [InlineData("#", "1")]
        [InlineData("(", "2")]
        [InlineData("\"", "3")]
        [InlineData("%", "4")]
        [InlineData("\f", "5")] // Form feed char
        [InlineData("'", "6")]
        [InlineData("$", "7")]
        [InlineData("&", "8")]
        [InlineData("!", "9")]
        public void FixNumpadChars_ShouldConvertSpecialToDigit(string input, string expected)
        {
            // Act
            string result = FixNumpadChars(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void FixNumpadChars_ShouldIgnoreNonMappedCharacters()
        {
            // Arrange
            string input = "A-B#C(%)";
            string expected = "A0B1C24)";

            // Act
            string result = FixNumpadChars(input);

            // Assert
            result.Should().Be(expected);
        }

        /// <summary>
        /// Simulates Conquer's numpad password encoding scheme.
        /// </summary>
        private static string FixNumpadChars(string input)
        {
            StringBuilder sb = new();
            foreach (char c in input)
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

        #endregion

        #region ReadExactAsync Tests

        [Fact]
        public async Task ReadExactAsync_ShouldReturnTrue_WhenExactBytesAvailable()
        {
            // Arrange
            byte[] content = Encoding.ASCII.GetBytes("HelloWorld");
            MemoryStream stream = new(content);
            byte[] buffer = new byte[10];

            // Act
            bool result = await ReadExactAsync(stream, buffer, 10, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            buffer.Should().BeEquivalentTo(content);
        }

        [Fact]
        public async Task ReadExactAsync_ShouldReturnFalse_WhenStreamEndsEarly()
        {
            // Arrange
            byte[] content = Encoding.ASCII.GetBytes("Short");
            MemoryStream stream = new(content);
            byte[] buffer = new byte[10];

            // Act
            bool result = await ReadExactAsync(stream, buffer, 10, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ReadExactAsync_ShouldReadInChunks()
        {
            // Arrange
            byte[] content = Encoding.ASCII.GetBytes("ChunkTest");
            ThrottledStream partialStream = new(content, 2); // Simulate chunked socket stream
            byte[] buffer = new byte[9];

            // Act
            bool result = await ReadExactAsync(partialStream, buffer, 9, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            buffer.Should().BeEquivalentTo(content);
        }

        /// <summary>
        /// Reads exactly <paramref name="cnt"/> bytes from the stream unless EOF is hit early.
        /// </summary>
        private static async Task<bool> ReadExactAsync(Stream s, byte[] buf, int cnt, CancellationToken ct)
        {
            int offset = 0;
            while (offset < cnt)
            {
                int n = await s.ReadAsync(buf.AsMemory(offset, cnt - offset), ct);
                if (n == 0)
                {
                    return false;
                }

                offset += n;
            }
            return true;
        }

        /// <summary>
        /// Mimics a network stream with limited read chunk size.
        /// </summary>
        private class ThrottledStream(byte[] data, int maxBytesPerRead) : Stream
        {
            private readonly MemoryStream _inner = new(data);
            private readonly int _maxBytesPerRead = maxBytesPerRead;

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

            public override void Flush() => _inner.Flush();

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesToRead = Math.Min(_maxBytesPerRead, count);
                return _inner.Read(buffer, offset, bytesToRead);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                int bytesToRead = Math.Min(_maxBytesPerRead, count);
                return _inner.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        #endregion

        #region LoginHandshake Tests
        [Fact]
        public async Task HandleHandshakeAsync_FullSuccessFlow_ShouldSendSeedAndRespond()
        {
            // Arrange
            string username = "testuser";
            string password = "testpass";
            uint seed = 12345678; // Seed used to build expected login packet

            (LoginClientSession session, NetworkStream clientStream, TcpClient client) = await CreateLoopbackTcpSession(username, password, seed);

            // Act: Start the server logic in the background
            Task serverTask = session.HandleHandshakeAsync(CancellationToken.None);

            // Step 1: Receive and decrypt the seed packet
            byte[] encryptedSeed = new byte[SeedResponsePacket.PacketLength];
            int seedBytesRead = await clientStream.ReadAsync(encryptedSeed, 0, encryptedSeed.Length);

            // Decrypt seed
            LoginCipher cipher = new();
            cipher.Decrypt(encryptedSeed, seedBytesRead);

            SeedResponsePacket seedPacket = ParseSeedResponsePacket(encryptedSeed);
            uint receivedSeed = seedPacket.Seed;

            // Step 2: Build and send the encrypted login packet
            byte[] encryptedLogin = BuildAndEncryptLoginPacket(username, password, receivedSeed);
            await clientStream.WriteAsync(encryptedLogin);
            await clientStream.FlushAsync();

            // Step 3: Receive the authentication response
            int expectedResponseLength = new AuthResponsePacket().Length;
            byte[] authResponse = new byte[expectedResponseLength];
            int authRead = await clientStream.ReadAsync(authResponse, 0, authResponse.Length);

            // Wait for server task to complete
            await serverTask;

            // Assert: Response should be successful
            authResponse[..authRead].Should().NotBeEmpty("a response packet should have been received");
            BitConverter.ToUInt32(authResponse, 4).Should().Be(AuthResponsePacket.RESPONSE_VALID, "the login credentials were correct");
        }


        /// <summary>
        /// Custom memory stream that supports both read and write tracking.
        /// </summary>
        private class BiDirectionalTestStream : NetworkStream
        {
            private readonly MemoryStream _input = new();
            private readonly MemoryStream _output = new();

            public BiDirectionalTestStream() : base(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), ownsSocket: true)
            {
            }

            public void PreloadForRead(byte[] data)
            {
                _input.Write(data, 0, data.Length);
                _input.Position = 0;
            }

            public byte[] GetWrites()
            {
                return _output.ToArray();
            }

            public override bool CanRead => _input.CanRead;
            public override bool CanWrite => true;
            public override bool CanSeek => false;

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _input.Read(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _input.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _output.Write(buffer, offset, count);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _output.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
            }

            public override void Flush() => _output.Flush();
            public override Task FlushAsync(CancellationToken cancellationToken) => _output.FlushAsync(cancellationToken);
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        [Fact]
        public async Task HandleHandshakeAsync_ShouldReject_WhenUsernameIsWrong()
        {
            string username = "wronguser";
            string password = "testpass";
            uint seed = 22334455;

            (LoginClientSession session, NetworkStream clientStream, TcpClient _) = await CreateLoopbackTcpSession(username, password, seed);
            await session.HandleHandshakeAsync(CancellationToken.None);

            byte[] buffer = new byte[clientStream.Socket.Available];
            int read = await clientStream.ReadAsync(buffer, 0, buffer.Length);
            BitConverter.ToUInt32(buffer, 4).Should().Be(AuthResponsePacket.RESPONSE_INVALID_ACCOUNT);
        }

        [Fact]
        public async Task HandleHandshakeAsync_ShouldReject_WhenPasswordIsWrong()
        {
            string username = "testuser";
            string password = "wrongpass";
            uint seed = 12345678;

            (LoginClientSession session, NetworkStream clientStream, TcpClient _) = await CreateLoopbackTcpSession(username, password, seed);
            await session.HandleHandshakeAsync(CancellationToken.None);

            byte[] buffer = new byte[clientStream.Socket.Available];
            int read = await clientStream.ReadAsync(buffer, 0, buffer.Length);
            BitConverter.ToUInt32(buffer, 4).Should().Be(AuthResponsePacket.RESPONSE_INVALID);
        }

        [Fact]
        public async Task HandleHandshakeAsync_ShouldAccept_WhenUsernameAndPasswordAreCorrect()
        {
            string username = "testuser";
            string password = "testpass";
            uint seed = 98765432;

            (LoginClientSession session, NetworkStream clientStream, TcpClient _) = await CreateLoopbackTcpSession(username, password, seed);
            await session.HandleHandshakeAsync(CancellationToken.None);

            byte[] buffer = new byte[clientStream.Socket.Available];
            int read = await clientStream.ReadAsync(buffer, 0, buffer.Length);
            BitConverter.ToUInt32(buffer, 4).Should().Be(AuthResponsePacket.RESPONSE_VALID);
        }

        #endregion

        #region Test Helpers
        //private static async Task<(LoginClientSession, NetworkStream, TcpClient)> CreateLoopbackTcpSession(string username, string password, uint seed)
        //{
        //    TcpListener listener = new(IPAddress.Loopback, 0);
        //    listener.Start();

        //    int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        //    TcpClient client = new();
        //    Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
        //    await client.ConnectAsync(IPAddress.Loopback, port);
        //    TcpClient server = await acceptTask;

        //    listener.Stop();

        //    // Build and encrypt login packet using correct seed
        //    byte[] encryptedBlob = GenerateEncryptedPasswordBlob(username, password, seed);
        //    byte[] rawLoginPacket = BuildRawLoginRequestPacket(username, encryptedBlob);
        //    new LoginCipher().Encrypt(rawLoginPacket, rawLoginPacket.Length);

        //    // Write it into the stream
        //    NetworkStream clientStream = client.GetStream();
        //    await clientStream.WriteAsync(rawLoginPacket, 0, rawLoginPacket.Length);
        //    await clientStream.FlushAsync();

        //    ILogger<LoginClientSession> logger = Mock.Of<ILogger<LoginClientSession>>();
        //    LoginClientSession session = new(server, Mock.Of<IAccountService>(), logger);
        //    return (session, clientStream, client);
        //}

        private static byte[] BuildAndEncryptLoginPacket(string username, string password, uint seed)
        {
            byte[] encryptedBlob = GenerateEncryptedPasswordBlob(username, password, seed);
            byte[] raw = BuildRawLoginRequestPacket(username, encryptedBlob);
            new LoginCipher().Encrypt(raw, raw.Length);
            return raw;
        }

        private static byte[] BuildRawLoginRequestPacket(string username, byte[] passwordBlob, ushort packetId = 0x0424)
        {
            const int PacketSize = 148; // CO login packet fixed length
            byte[] buffer = new byte[PacketSize];

            // Header: [length, packetId]
            BitConverter.GetBytes((ushort)PacketSize).CopyTo(buffer, 0);
            BitConverter.GetBytes(packetId).CopyTo(buffer, 2);

            // Username (ASCII, 16 bytes, null-padded)
            byte[] usernameBytes = Encoding.ASCII.GetBytes(username);
            Array.Copy(usernameBytes, 0, buffer, 4, Math.Min(16, usernameBytes.Length));

            // Password blob (16 bytes @ offset 132)
            Array.Copy(passwordBlob, 0, buffer, 132, Math.Min(16, passwordBlob.Length));

            return buffer;
        }

        private static byte[] GenerateEncryptedPasswordBlob(string username, string password, uint seed)
        {
            byte[] rc5Key = RC5KeyGenerator.GenerateFromSeed((int)seed);
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            byte[] conquerEncrypted = new ConquerPasswordCryptographer(username).Encrypt(passwordBytes, password.Length);
            if (conquerEncrypted.Length != 16)
            {
                Array.Resize(ref conquerEncrypted, 16);
            }
            return new RC5Cipher(rc5Key).Encrypt(conquerEncrypted);
        }

        private static LoginRequestPacket CreateLoginRequestPacket(string username, string password, uint seed)
        {
            byte[] encryptedBlob = GenerateEncryptedPasswordBlob(username, password, seed);
            byte[] rawPacket = BuildRawLoginRequestPacket(username, encryptedBlob);
            return LoginRequestPacket.Parse(rawPacket);
        }

        private static TcpClient CreateMockTcpClient(Stream stream)
        {
            Mock<Socket> socketMock = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketMock.SetupProperty(s => s.NoDelay);

            Mock<TcpClient> tcpMock = new();
            tcpMock.Setup(c => c.GetStream()).Returns((NetworkStream)stream);
            tcpMock.Setup(c => c.Client).Returns(socketMock.Object);

            return tcpMock.Object;
        }

        private static async Task<byte[]> ReadResponseAsync(NetworkStream stream, int expectedLength = 32, int timeoutMs = 2000)
        {
            byte[] buffer = new byte[expectedLength];
            using CancellationTokenSource cts = new(timeoutMs);

            int offset = 0;
            while (offset < expectedLength)
            {
                int read = await stream.ReadAsync(buffer, offset, expectedLength - offset, cts.Token);
                if (read == 0)
                {
                    break; // end of stream
                }

                offset += read;
            }

            return buffer[..offset];
        }

        private static SeedResponsePacket ParseSeedResponsePacket(byte[] buffer)
        {
            int packetID = 1059;
            if (buffer.Length < 8)
            {
                throw new ArgumentException("Invalid seed packet length.");
            }

            ushort len = BitConverter.ToUInt16(buffer, 0);
            ushort type = BitConverter.ToUInt16(buffer, 2);
            if (len != 8 || type != packetID)
            {
                throw new InvalidOperationException("Invalid seed packet structure.");
            }

            uint seed = BitConverter.ToUInt32(buffer, 4);
            return new SeedResponsePacket(seed);
        }

        #endregion
    }
}
