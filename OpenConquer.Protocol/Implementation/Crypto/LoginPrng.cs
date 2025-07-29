
namespace OpenConquer.Protocol.Implementation.Crypto
{
    /// <summary>
    /// Emulates the MSVCRT-style PRNG used in the login handshake.
    /// </summary>
    public class LoginPrng(int seed)
    {
        private int _seed = seed;

        /// <summary>
        /// Returns the next pseudo-random value (15-bit, unsigned).
        /// </summary>
        public short Next()
        {
            _seed = _seed * 214013 + 2531011;
            return (short)((_seed >> 16) & 0x7FFF);
        }
    }
}
