
namespace OpenConquer.Infrastructure.Enums
{
    public enum AccountPermissionCode : uint
    {
        Error = 0,
        Player = 1,
        Helper = 2,
        Moderator = 3,
        PM = 4,
        GM = 5,
        Banned = 255
    }
}
