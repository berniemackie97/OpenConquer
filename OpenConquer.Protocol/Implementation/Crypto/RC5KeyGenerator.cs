
namespace OpenConquer.Protocol.Implementation.Crypto
{
    /// <summary>
    /// Helper to generate a 16-byte RC5 key from a login seed using the legacy PRNG.
    /// </summary>
    public static class RC5KeyGenerator
    {
        public static byte[] GenerateFromSeed(int seed)
        {
            LoginPrng prng = new(seed);
            byte[] key = new byte[16];
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)prng.Next();
            }

            return key;
        }
    }
}
