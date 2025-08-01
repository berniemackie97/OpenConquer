namespace OpenConquer.Protocol.Crypto
{
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
