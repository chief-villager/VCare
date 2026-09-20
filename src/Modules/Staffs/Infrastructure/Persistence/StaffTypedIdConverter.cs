using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Infrastructure.Persistence
{
    public class StaffTypedIdConverter : ValueConverter<StaffId, Guid>
    {
        public StaffTypedIdConverter() : base(x => x.Value, x => new StaffId(x))
        {
        }
    }
}
