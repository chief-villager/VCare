using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using src.Modules.CareHomes.Application.Contract;
using src.Modules.CareHomes.Domain.Entity;
using src.Modules.CareHomes.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace src.Modules.CareHomes.Infrastructure.Repository
{
    internal class CareHomeRepository(CareHomeDbContext dbContext) : ICareHomeRepository
    {
        public async Task AddAsync(CareHome careHome, CancellationToken cancellationToken)
        {
            await dbContext.CareHomes.AddAsync(careHome, cancellationToken);
           
        }
        public async Task<CareHome?> GetByIdAsync(CareHomeId id, CancellationToken cancellationToken)
        {
            return await dbContext.CareHomes.FindAsync(new object[] { id }, cancellationToken);
        }

        public void UpdateAsync(CareHome careHome)
        {
            dbContext.Update(careHome);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }

      
    }
}