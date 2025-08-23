using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Infrastructure.Persistence.Context;

namespace OpenConquer.Infrastructure.Services
{
    public class AccountService(DataContext accountDataContext) : IAccountService
    {
        private readonly DataContext _accountDataContext = accountDataContext;

        public async Task UpdateHashAsync(uint uid, uint newHash, CancellationToken ct = default)
        {
            Models.AccountEntity acct = await _accountDataContext.Accounts.FindAsync(new object[] { uid }, ct) ?? throw new KeyNotFoundException($"No account {uid}");
            acct.Hash = newHash;
            acct.Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await _accountDataContext.SaveChangesAsync(ct);
        }

        public async Task<Account> CreateAsync(Account newAccount, CancellationToken ct = default)
        {
            Models.AccountEntity ent = newAccount.Adapt<Models.AccountEntity>();

            await _accountDataContext.Accounts.AddAsync(ent, ct);
            await _accountDataContext.SaveChangesAsync(ct);

            return ent.Adapt<Account>();
        }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            Models.AccountEntity? entity = await _accountDataContext.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username);

            if (entity == null)
            {
                return null;
            }

            return entity.Adapt<Account>();
        }

        public async Task<uint> PullKeyAsync(uint hash, CancellationToken ct)
        {
            Models.AccountEntity? acct = await _accountDataContext.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Hash == hash, ct);
            return acct?.UID ?? 0;
        }
    }
}
