using OpenConquer.Domain.Entities;

namespace OpenConquer.Domain.Contracts
{
    public interface IAccountService
    {
        Task<Account?> GetByUsernameAsync(string username);
        Task<uint> PullKeyAsync(uint hash, CancellationToken ct);
    }
}
