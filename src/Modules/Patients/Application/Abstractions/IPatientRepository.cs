using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Application.Abstractions;

internal interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Patients of the caller's care home; the context's query filter does the scoping.</summary>
    Task<IReadOnlyList<Patient>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
