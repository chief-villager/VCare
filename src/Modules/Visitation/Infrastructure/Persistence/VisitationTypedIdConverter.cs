using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Visitation.Infrastructure.Persistence;

public class VisitTypedIdConverter : ValueConverter<VisitId, Guid>
{
    public VisitTypedIdConverter() : base(x => x.Value, x => new VisitId(x))
    {
    }
}

// The patient id is stored as a plain value: this module references the patient
// by id only, with no navigation into the Patients module.
public class VisitPatientTypedIdConverter : ValueConverter<PatientId, Guid>
{
    public VisitPatientTypedIdConverter() : base(x => x.Value, x => new PatientId(x))
    {
    }
}
