using Microsoft.EntityFrameworkCore;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Domain.Entities;
using VCare.Modules.Patients.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Infrastructure.Repositories;

internal sealed class PatientRepository(PatientsDbContext context) : IPatientRepository
{
    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patientId = new PatientId(id);
        return await context.Patients
            .Include(p => p.Allergies)
            .Include(p => p.Absences)
            .Include(p => p.Weights)
            .Include(p => p.Attorneys)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> ListAsync(CancellationToken cancellationToken = default) =>
        await context.Patients
            .AsNoTracking()
            // For each resident's latest weight on the list.
            .Include(p => p.Weights)
            .AsSplitQuery()
            .OrderBy(p => p.Lastname)
            .ThenBy(p => p.Firstname)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default) =>
        await context.Patients.AddAsync(patient, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);

    public async Task<bool> NhsNumberInUseAsync(string nhsNumber, Guid exceptPatientId, CancellationToken cancellationToken = default)
    {
        var except = new PatientId(exceptPatientId);
        return await context.Patients.AnyAsync(p => p.NhsNumber == nhsNumber && p.Id != except, cancellationToken);
    }

    public async Task<bool> OwnsStorageKeyAsync(Guid patientId, string storageKey, CancellationToken cancellationToken = default)
    {
        var id = new PatientId(patientId);
        return await context.Patients
            .Where(p => p.Id == id)
            .AnyAsync(p => p.ProfilePictureUrl == storageKey
                        || p.Documents.Any(d => d.StorageKeyName == storageKey), cancellationToken);
    }
}
