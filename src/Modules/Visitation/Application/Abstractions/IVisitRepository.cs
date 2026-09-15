using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Visitation.Application.Abstractions;

internal interface IVisitRepository
{
    Task<Visit?> GetByIdAsync(VisitId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Visit>> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default);

    // Every visit expected or in progress on a given day, for the front desk.
    Task AddAsync(Visit visit, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
