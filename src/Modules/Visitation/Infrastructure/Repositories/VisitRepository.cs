using Microsoft.EntityFrameworkCore;
using VCare.Modules.Visitation.Application.Abstractions;
using VCare.Modules.Visitation.Domain.Entities;
using VCare.Modules.Visitation.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Visitation.Infrastructure.Repositories;

internal sealed class VisitRepository(VisitationDbContext context) : IVisitRepository
{
    public async Task<Visit?> GetByIdAsync(VisitId id, CancellationToken cancellationToken = default) =>
        await context.Visits
            .Include(v => v.MedicationTask)
            .Include(v => v.FeedingTask)
            .Include(v => v.PersonalcareTask)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Visit>> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default) =>
        await context.Visits
            .Include(v => v.MedicationTask)
            .Include(v => v.FeedingTask)
            .Include(v => v.PersonalcareTask)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.CheckedInAt)
            .ToListAsync(cancellationToken);



    public async Task AddAsync(Visit visit, CancellationToken cancellationToken = default) =>
        await context.Visits.AddAsync(visit, cancellationToken);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);
}
