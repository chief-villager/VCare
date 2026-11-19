using Medications.Application.Abstracts;
using Medications.Domain.Entities;
using Medications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;

namespace Medications.Infrastructure.Repositories
{
    internal class MedicationAdministrationRepository(MedicationDbContext medicationDbContext) : IMedicalAdministrationRepository
    {

        public async Task AddAsync(MedicationAdministration administration)
        {
            await medicationDbContext.MedicationAdministrations.AddAsync(administration);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await medicationDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<HashSet<(Guid OrderId, DateTime ScheduledFor)>> RecordedSlotsForAllCareHomesAsync(
            IReadOnlyCollection<Guid> orderIds, DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            var ids = orderIds.Select(id => new MedicationOrderId(id)).ToList();
            var rows = await medicationDbContext.MedicationAdministrations
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => ids.Contains(a.MedicationOrderId)
                         && a.ScheduledFor != null
                         && a.ScheduledFor >= from
                         && a.ScheduledFor <= to)
                .Select(a => new { a.MedicationOrderId, a.ScheduledFor })
                .ToListAsync(cancellationToken);
            return rows.Select(r => (r.MedicationOrderId.Value, r.ScheduledFor!.Value)).ToHashSet();
        }

        public void DiscardPendingChanges() => medicationDbContext.ChangeTracker.Clear();

        public async Task<MedicationAdministration?> Find(Guid orderId, DateTime scheduledFor)
        {
            var medicationOrderId = new MedicationOrderId(orderId);
            return await medicationDbContext.MedicationAdministrations.FirstOrDefaultAsync
            ( x => x.MedicationOrderId == medicationOrderId && x.ScheduledFor == scheduledFor);
        }

        // Scheduled doses are found by their slot. PRN doses have no slot, so they
        // are found by when they were given; without this branch they never reached
        // the MAR chart at all.
        public async Task<List<MedicationAdministration>> ForResidentBetweenAsync(Guid patientId, DateOnly from, DateOnly to,
            CancellationToken cancellationToken)
        {
            var patient = new PatientId(patientId);
            var start = from.ToDateTime(TimeOnly.MinValue);
            var end = to.ToDateTime(TimeOnly.MaxValue);
            return await medicationDbContext.MedicationAdministrations
                .Where(a => a.PatientId == patient
                         && ((a.ScheduledFor != null && a.ScheduledFor >= start && a.ScheduledFor <= end)
                          || (a.ScheduledFor == null && a.AdministeredAt >= start && a.AdministeredAt <= end)))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DateTime>> PrnGivenTimesAsync(Guid orderId, DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            var medicationOrderId = new MedicationOrderId(orderId);
            return await medicationDbContext.MedicationAdministrations
                .Where(a => a.MedicationOrderId == medicationOrderId
                         && a.ScheduledFor == null
                         && a.OutcomeCodeId == OutcomeCode.TakenId
                         && a.AdministeredAt >= from
                         && a.AdministeredAt <= to)
                .Select(a => a.AdministeredAt!.Value)
                .ToListAsync(cancellationToken);
        }
    }
}
