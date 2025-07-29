
namespace OpenConquer.Protocol.Interface.Crypto
{
    /// <summary>
    /// Conquer-style packet cipher for encrypting/decrypting packet data streams.
    /// </summary>
    public interface IPacketCipher
    {
        /// <summary>
        /// Encrypts the given buffer in-place, up to the specified length.
        /// </summary>
        void Encrypt(byte[] buffer, int length);

        /// <summary>
        /// Decrypts the given buffer in-place, up to the specified length.
        /// </summary>
        void Decrypt(byte[] buffer, int length);
    }
}
