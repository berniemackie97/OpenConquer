using Mapster;
using OpenConquer.Domain.Entities;
using OpenConquer.Infrastructure.Models;

namespace OpenConquer.Infrastructure.Mapping
{
    public static class MapsterConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig<AccountEntity, Account>.NewConfig();
            TypeAdapterConfig<LevelStatEntity, LevelStat>.NewConfig();
        }
    }
}
