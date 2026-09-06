using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Staffs.Domain.Entity;

namespace Staffs.Application.Abstraction
{
    internal interface IStaffRepository
    {
        Task<Staff?> GetByIdAsync(VCare.SharedKernel.Abstractions.StaffId id, CancellationToken cancellationToken = default);
        Task AddAsync(Staffs.Domain.Entity.Staff staff, CancellationToken cancellationToken = default);
        void Update(Staffs.Domain.Entity.Staff staff);
        Task<int> SaveChangesAsync( CancellationToken cancellationToken = default);

    }
}