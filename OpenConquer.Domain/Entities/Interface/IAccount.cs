
using OpenConquer.Domain.Enums;

namespace OpenConquer.Domain.Entities.Interface
{
    public interface IAccount
    {
        uint UID { get; }
        string Username { get; set; }
        string Password { get; set; }
        string EMail { get; set; }
        string Question { get; set; }
        string Answer { get; set; }
        PlayerPermission Permission { get; set; }
        uint Hash { get; set; }
        uint Timestamp { get; set; }

        void AllowLogin(bool updateTimestamp = true);
    }
}
