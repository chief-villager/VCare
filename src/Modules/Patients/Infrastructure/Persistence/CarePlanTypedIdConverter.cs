using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace Patients.Infrastructure.Persistence;

public class CarePlanTypedIdConverter : ValueConverter<CarePlanId, Guid>
{
    public CarePlanTypedIdConverter() : base(x => x.Value, x => new CarePlanId(x))
    {
    }
}
