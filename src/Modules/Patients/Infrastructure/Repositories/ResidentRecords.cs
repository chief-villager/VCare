using Microsoft.EntityFrameworkCore;
using VCare.Modules.Patients.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Infrastructure.Repositories;

// Read through the patient, so the care-home query filter applies: another
// home's resident has no allergies as far as the caller can tell.
internal sealed class ResidentAllergies(PatientsDbContext context) : IResidentAllergies
{
    public async Task<IReadOnlyList<string>> ActiveSubstancesAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var id = new PatientId(patientId);
        return await context.Patients
            .Where(p => p.Id == id)
            .SelectMany(p => p.Allergies)
            .Where(a => a.RemovedAt == null)
            .Select(a => a.Substance)
            .ToListAsync(cancellationToken);
    }
}

// Read straight from the absences, not through the patient: the missed-dose
// recorder runs with no caller, so there is no care home to filter by. Callers
// pass resident ids they already read from their own scoped data.
internal sealed class ResidentAbsences(PatientsDbContext context) : IResidentAbsences
{
    public async Task<IReadOnlyList<ResidentAbsence>> OverlappingAsync(IReadOnlyCollection<Guid> patientIds,
        DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        if (patientIds.Count == 0)
            return [];
        var ids = patientIds.Select(id => new PatientId(id)).ToList();
        var away = await context.Set<Domain.Entities.PatientAbsence>()
            .AsNoTracking()
            .Where(a => ids.Contains(a.PatientId) && a.StartedAt <= to && (a.EndedAt == null || a.EndedAt >= from))
            .Select(a => new ResidentAbsence(a.PatientId.Value, a.Kind, a.StartedAt, a.EndedAt))
            .ToListAsync(cancellationToken);

        // A resident who was discharged or died is away from then on, for good.
        // IgnoreQueryFilters for the same reason as above: no caller, no care home.
        var left = await context.Patients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.LeftAt != null && p.LeftAt <= to)
            .Select(p => new { p.Id, p.Residency, LeftAt = p.LeftAt!.Value })
            .ToListAsync(cancellationToken);
        away.AddRange(left.Select(p => new ResidentAbsence(p.Id.Value,
            p.Residency == Domain.Entities.ResidencyStatus.Deceased ? AbsenceKind.Deceased : AbsenceKind.Discharged,
            p.LeftAt, null)));
        return away;
    }
}
