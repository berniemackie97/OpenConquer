
using OpenConquer.Infrastructure.Enums;

namespace OpenConquer.Infrastructure.Models
{
    public class AccountEntity
    {
        public uint UID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string EMailver { get; set; } = string.Empty;
        public int EMailstatus { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public AccountPermissionCode Permission { get; set; }
        public uint Hash { get; set; }
        public uint Timestamp { get; set; }
    }
}
