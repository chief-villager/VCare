using Microsoft.EntityFrameworkCore;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Domain.Entities;
using VCare.Modules.Patients.Infrastructure.Persistence;

namespace VCare.Modules.Patients.Infrastructure.Repositories;

internal sealed class PatientRepository(PatientsDbContext context) : IPatientRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default) =>
        await context.Patients.AddAsync(patient, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);
}
