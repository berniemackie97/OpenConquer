
namespace OpenConquer.AccountServer.Session
{
    public interface ILoginKeyProvider
    {
        uint NextKey();
    }

    public class LockingLoginKeyProvider : ILoginKeyProvider
    {
        private uint _current;
        private readonly object _lock = new();

        public uint NextKey()
        {
            lock (_lock)
            {
                // wrap from 0 -> 1 (so first key is 1)
                if (_current == uint.MaxValue)
                {
                    _current = 0;
                }

                return ++_current;
            }
        }
    }
}
