using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace Medications.Infrastructure.Persistence
{
    // Ids owned by other modules are stored as plain values: this module references
    // the care home and the patient by id only, with no navigation into either.
    public class CareHomeIdConverter : ValueConverter<CareHomeId, Guid>
    {
        public CareHomeIdConverter() : base(x => x.Value, x => new CareHomeId(x))
        {
        }
    }

    public class PatientIdConverter : ValueConverter<PatientId, Guid>
    {
        public PatientIdConverter() : base(x => x.Value, x => new PatientId(x))
        {
        }
    }
}
