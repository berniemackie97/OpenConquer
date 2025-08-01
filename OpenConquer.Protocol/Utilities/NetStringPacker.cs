using System.Text;

namespace OpenConquer.Protocol.Utilities
{
    public sealed class NetStringPacker
    {
        private readonly List<string> _values;

        public NetStringPacker()
        {
            _values = [];
        }

        public NetStringPacker(int stringCount)
        {
            _values = new List<string>(stringCount);
            for (int i = 0; i < stringCount; i++)
            {
                _values.Add(string.Empty);
            }
        }

        public static NetStringPacker Parse(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                return new NetStringPacker();
            }

            int offset = 0;
            int count = buffer[offset++];
            NetStringPacker packer = new();
            packer._values.Capacity = count;

            for (int i = 0; i < count; i++)
            {
                int length = buffer[offset++];
                string str = Encoding.Default.GetString(buffer.Slice(offset, length));
                offset += length;
                packer._values.Add(str);
            }
            return packer;
        }

        public bool AddString(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > 255)
            {
                return false;
            }

            _values.Add(value);
            return true;
        }

        public bool GetString(int index, out string value)
        {
            if (index < 0 || index >= _values.Count)
            {
                value = string.Empty;
                return false;
            }
            value = _values[index];
            return true;
        }

        public string GetString(int index)
        {
            if (index < 0 || index >= _values.Count)
            {
                return string.Empty;
            }

            return _values[index];
        }

        public bool SetString(int index, string value)
        {
            if (index < 0 || index >= _values.Count)
            {
                return false;
            }

            _values[index] = value;
            return true;
        }

        public void Clear() => _values.Clear();

        public bool Contains(int index) => index >= 0 && index < _values.Count;

        public int Capacity
        {
            get => _values.Capacity;
            set => _values.Capacity = value;
        }

        public int Count => _values.Count;

        public int Length => 1 + _values.Count + _values.Sum(s => (s?.Length ?? 0));

        public byte[] ToArray()
        {
            byte[] buffer = new byte[Length];
            int offset = 1;
            buffer[0] = (byte)_values.Count;

            for (int i = 0; i < _values.Count; i++)
            {
                string s = _values[i] ?? string.Empty;
                byte[] bytes = Encoding.Default.GetBytes(s);
                buffer[offset++] = (byte)bytes.Length;
                Array.Copy(bytes, 0, buffer, offset, bytes.Length);
                offset += bytes.Length;
            }

            return buffer;
        }

        public static implicit operator byte[](NetStringPacker packer) => packer.ToArray();
    }
}
