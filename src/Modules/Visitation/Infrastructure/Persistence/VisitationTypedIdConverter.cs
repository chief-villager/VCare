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

// The care home and the staff member are referenced by id only, with no
// navigation out of this module.
public class VisitCareHomeTypedIdConverter : ValueConverter<CareHomeId, Guid>
{
    public VisitCareHomeTypedIdConverter() : base(x => x.Value, x => new CareHomeId(x))
    {
    }
}

public class VisitStaffTypedIdConverter : ValueConverter<StaffId, Guid>
{
    public VisitStaffTypedIdConverter() : base(x => x.Value, x => new StaffId(x))
    {
    }
}
