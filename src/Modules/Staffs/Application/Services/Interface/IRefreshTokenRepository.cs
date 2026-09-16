using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Staffs.Domain.Entity;

namespace Staffs.Application.Services.Interface
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken);
        Task<IEnumerable<RefreshToken>> GetAllActiveTokenAsync(Guid TokenFamilyId, CancellationToken cancellationToken);
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        
    }
}