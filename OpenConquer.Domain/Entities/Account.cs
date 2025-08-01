using OpenConquer.Domain.Enums;

namespace OpenConquer.Domain.Entities
{
    public class Account
    {
        public uint UID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public PlayerPermission Permission { get; set; }
        public uint Hash { get; set; }
        public uint Timestamp { get; set; }

        public void AllowLogin(bool updateTimestamp = true)
        {
            if (updateTimestamp)
            {
                Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }
    }
}
