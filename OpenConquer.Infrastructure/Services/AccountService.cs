using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Infrastructure.Persistence.Context;

namespace OpenConquer.Infrastructure.Services
{
    public class AccountService(AccountDataContext accountDataContext) : IAccountService
    {
        private readonly AccountDataContext _accountDataContext = accountDataContext;

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
