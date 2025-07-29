using Mapster;
using OpenConquer.Domain.Entities;
using OpenConquer.Domain.Enums;
using OpenConquer.Infrastructure.Enums;
using OpenConquer.Infrastructure.Models;

namespace OpenConquer.Tests.Mapping
{
    public static class MapsterTestConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig<AccountEntity, Account>
                .NewConfig()
                .Map(dest => dest.Permission, src => (PlayerPermission)src.Permission);

            TypeAdapterConfig<Account, AccountEntity>
                .NewConfig()
                .Map(dest => dest.Permission, src => (AccountPermissionCode)src.Permission);
        }
    }
}
