using VCare.SharedKernel.Abstractions;
using Visitation.Domain.Entities;

namespace VCare.Modules.Visitation.Application.Abstractions;

internal interface IVisitRepository
{
    Task<Visit?> GetByIdAsync(VisitId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Visit>> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Visit> visits, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
