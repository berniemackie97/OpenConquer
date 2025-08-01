using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Infrastructure.Models;
using OpenConquer.Infrastructure.Persistence.Context;

namespace OpenConquer.Infrastructure.Services
{
    public class CharacterService(GameDataContext gameDataContext) : ICharacterService
    {
        private readonly GameDataContext _gameDataContext = gameDataContext;

        public async Task<Character?> LoadAsync(uint uid, CancellationToken ct)
        {
            CharacterEntity? entity = await _gameDataContext.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.UID == uid, ct);

            if (entity == null)
            {
                return null;
            }

            return entity.Adapt<Character>();
        }

        public async Task SaveAsync(Character character, CancellationToken ct)
        {
            CharacterEntity entity = character.Adapt<CharacterEntity>();

            _gameDataContext.Entry(entity).State = EntityState.Modified;

            await _gameDataContext.SaveChangesAsync(ct);
        }

        public async Task<bool> PullLoginAsync(Character user, CancellationToken ct)
        {
            CharacterEntity? entity = await _gameDataContext.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.UID == user.UID, ct);
            if (entity == null)
            {
                return false;
            }

            entity.Adapt(user);
            return true;
        }

        public async Task CreateAsync(Character character, CancellationToken ct)
        {
            CharacterEntity entity = character.Adapt<CharacterEntity>();

            await _gameDataContext.Characters.AddAsync(entity, ct);
            await _gameDataContext.SaveChangesAsync(ct);

            character.UID = entity.UID;
            character.MapID = entity.MapID;
            character.X = entity.X;
            character.Y = entity.Y;
        }

        public async Task<bool> NameExistsAsync(string name, CancellationToken ct)
        {
            return await _gameDataContext.Characters.AsNoTracking().AnyAsync(c => c.Name == name, ct);
        }

    }
}
