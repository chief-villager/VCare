using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Staffs.Application.Abstraction;
using Staffs.Domain.Entity;
using Staffs.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Infrastructure.Repositories
{
    internal class StaffRepository(StaffDbContext dbContext) : IStaffRepository
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

        public async Task<Staff?> GetStaffWithUserNameAsync(string userName, CancellationToken token)
        {
            return await dbContext.Staffs.FirstOrDefaultAsync( x => x.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}