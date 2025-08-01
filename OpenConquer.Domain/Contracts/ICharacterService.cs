using OpenConquer.Domain.Entities;

namespace OpenConquer.Domain.Contracts
{
    public interface ICharacterService
    {
        Task<Character?> LoadAsync(uint uid, CancellationToken ct);
        Task SaveAsync(Character character, CancellationToken ct);
        Task<bool> PullLoginAsync(Character user, CancellationToken ct);
        Task CreateAsync(Character character, CancellationToken ct);
        Task<bool> NameExistsAsync(string name, CancellationToken ct);
    }
}
