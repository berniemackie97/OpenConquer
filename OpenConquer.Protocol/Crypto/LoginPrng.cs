namespace OpenConquer.Protocol.Crypto
{
    public class LoginPrng(int seed)
    {
        private int _seed = seed;

        public short Next()
        {
            _seed = _seed * 214013 + 2531011;
            return (short)(_seed >> 16 & 32767);
        }
    }
}
