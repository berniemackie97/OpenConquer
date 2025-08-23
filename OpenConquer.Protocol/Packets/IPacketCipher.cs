namespace OpenConquer.Protocol.Packets
{
    public interface IPacketCipher
    {
        void GenerateKeys(object[] seeds);
        void Decrypt(Span<byte> src, Span<byte> dst);
        void Encrypt(Span<byte> src, Span<byte> dst);

    }
}
