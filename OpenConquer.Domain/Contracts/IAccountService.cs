using OpenConquer.Domain.Entities;

namespace OpenConquer.Domain.Contracts
{
    public interface IAccountService
    {
        Task UpdateHashAsync(uint uid, uint newHash, CancellationToken ct = default);
        Task<Account> CreateAsync(Account newAccount, CancellationToken ct = default);
        Task<Account?> GetByUsernameAsync(string username);
        Task<uint> PullKeyAsync(uint hash, CancellationToken ct);
    }
}
