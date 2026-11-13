using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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

        /// <summary>
        /// returns an enumerable of medicationadministered already within the entered time frame
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IEnumerable<MedicationAdministration> ForResidentBetween(Guid patientId, DateOnly from, DateOnly to)
        {
            var patient = new PatientId(patientId);
            var result = medicationDbContext.MedicationAdministrations
                        .Where(a => a.PatientId == patient
                                && a.ScheduledFor >= from.ToDateTime(TimeOnly.MinValue)
                                && a.ScheduledFor <= to.ToDateTime(TimeOnly.MaxValue)).ToList();
            return result;
            
        }


        
    }
}