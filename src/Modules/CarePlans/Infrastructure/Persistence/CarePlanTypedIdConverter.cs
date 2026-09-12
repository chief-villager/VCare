using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Infrastructure.Persistence;

public class CarePlanTypedIdConverter : ValueConverter<CarePlanId, Guid>
{
    public CarePlanTypedIdConverter() : base(x => x.Value, x => new CarePlanId(x))
    {
    }
}

// The patient id is stored as a plain value: this module references the patient
// by id only, with no navigation into the Patients module.
public class PatientTypedIdConverter : ValueConverter<PatientId, Guid>
{
    public PatientTypedIdConverter() : base(x => x.Value, x => new PatientId(x))
    {
    }
}
