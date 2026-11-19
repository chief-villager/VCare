using Microsoft.EntityFrameworkCore;
using VCare.Modules.Patients.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Infrastructure.Repositories;

// The context's care-home query filter does the scoping: a patient in another
// home is simply not found, exactly like one that does not exist.
internal sealed class PatientDirectory(PatientsDbContext context) : IPatientDirectory
{
    public Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var id = new PatientId(patientId);
        return context.Patients.AnyAsync(p => p.Id == id, cancellationToken);
    }
}
