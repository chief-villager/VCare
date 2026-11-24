using Microsoft.EntityFrameworkCore;
using Staffs.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Infrastructure.Repositories
{
    // The context's care-home query filter does the scoping: staff in another
    // home are simply not found, exactly like staff who do not exist.
    internal sealed class StaffDirectory(StaffDbContext dbContext) : IStaffDirectory
    {
        public Task<bool> ExistsAsync(Guid staffId, CancellationToken cancellationToken)
        {
            var id = new StaffId(staffId);
            // Deactivated staff cannot be assigned a visit or witness a dose.
            return dbContext.Staffs.AnyAsync(s => s.Id == id && s.DeactivatedAt == null, cancellationToken);
        }
    }
}
