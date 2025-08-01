namespace OpenConquer.Protocol.Packets
{
    public interface IPacketCipher
    {
        void Encrypt(byte[] buffer, int length);
        void Decrypt(byte[] buffer, int length);
    }
}
