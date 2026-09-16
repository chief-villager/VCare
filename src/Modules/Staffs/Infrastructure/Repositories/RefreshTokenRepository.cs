using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Staffs.Application.Services.Interface;
using Staffs.Domain.Entity;
using Staffs.Infrastructure.Persistence;

namespace Staffs.Infrastructure.Repositories
{
    internal class RefreshTokenRepository(StaffDbContext dbContext):IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken)
        {
            var result = await dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
            return result;
        }

        public  async Task<IEnumerable<RefreshToken>> GetAllActiveTokenAsync(Guid TokenFamilyId, CancellationToken cancellationToken)
        {
           return await dbContext.RefreshTokens.Where(r => r.TokenFamily == TokenFamilyId && r.IsRevoked == false).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}