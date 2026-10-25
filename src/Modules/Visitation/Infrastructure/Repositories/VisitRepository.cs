using Microsoft.EntityFrameworkCore;
using VCare.Modules.Visitation.Application.Abstractions;
using VCare.Modules.Visitation.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;
using Visitation.Domain.Entities;

namespace VCare.Modules.Visitation.Infrastructure.Repositories;

internal sealed class VisitRepository(VisitationDbContext context) : IVisitRepository
{
    // The task records are owned by VisitRecord, so loading the records brings them along.
    public async Task<Visit?> GetByIdAsync(VisitId id, CancellationToken cancellationToken = default) =>
        await context.Visits
            .Include(v => v.VisitRecords)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Visit>> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default) =>
        await context.Visits
            .Include(v => v.VisitRecords)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDay)
            .ThenByDescending(v => v.VisitTime)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Visit> visits, CancellationToken cancellationToken = default) =>
        await context.Visits.AddRangeAsync(visits, cancellationToken);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);
}
