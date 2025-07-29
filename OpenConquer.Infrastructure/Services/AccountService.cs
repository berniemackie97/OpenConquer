using Mapster;
using Microsoft.EntityFrameworkCore;
using OpenConquer.Domain.Contracts;
using OpenConquer.Domain.Entities;
using OpenConquer.Infrastructure.Persistence;

namespace OpenConquer.Infrastructure.Services
{
    public class AccountService(AppDbContext db) : IAccountService
    {
        private readonly AppDbContext _db = db;

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            Models.AccountEntity? entity = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username);

            if (entity == null)
            {
                return null;
            }

            // Mapster maps to domain model
            return entity.Adapt<Account>();
        }
    }
}
