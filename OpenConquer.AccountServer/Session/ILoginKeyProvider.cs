using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenConquer.AccountServer.Session
{
    /// <summary>
    /// Thread‑safe provider of monotonically increasing 32‑bit login keys.
    /// </summary>
    public interface ILoginKeyProvider
    {
        /// <summary>
        /// Returns the next unique key (1, 2, 3, …).
        /// </summary>
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
                // wrap from 0→1 (so first key is 1)
                if (_current == uint.MaxValue)
                    _current = 0;
                return ++_current;
            }
        }
    }
}
