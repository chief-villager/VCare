using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Modules.CareHomes.Domain.Entity;
using VCare.SharedKernel.Abstractions;

namespace src.Modules.CareHomes.Application.Contract
{
    internal interface ICareHomeRepository
    {
        Task AddAsync(CareHome careHome, CancellationToken cancellationToken);
        void UpdateAsync(CareHome careHome);
        Task<CareHome?> GetByIdAsync(CareHomeId id, CancellationToken cancellationToken);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
        
    }
}