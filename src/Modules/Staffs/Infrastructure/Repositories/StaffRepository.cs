using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Staffs.Application.Abstraction;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Infrastructure.Repositories
{
    internal class StaffRepository(Staffs.Infrastructure.Persistence.StaffDbContext dbContext) : IStaffRepository
    {
        async Task IStaffRepository.AddAsync(Staff staff, CancellationToken cancellationToken)
        {
            _ = dbContext.Staffs.AddAsync(staff, cancellationToken);
            await Task.CompletedTask;
        }

        async Task<Staff?> IStaffRepository.GetByIdAsync(StaffId id, CancellationToken cancellationToken)
        {
            return await dbContext.Staffs.FindAsync(new object[] { id }, cancellationToken).AsTask();
        }

        void IStaffRepository.Update(Staff staff)
        {
            dbContext.Staffs.Update(staff);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}