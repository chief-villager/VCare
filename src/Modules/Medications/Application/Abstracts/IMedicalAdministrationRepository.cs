using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Medications.Domain.Entities;

namespace Medications.Application.Abstracts
{
    internal interface IMedicalAdministrationRepository
    {
        Task AddAsync(MedicationAdministration administration);
        Task<MedicationAdministration?> Find(Guid orderId, DateTime scheduledFor);
        IEnumerable<MedicationAdministration> ForResidentBetween(Guid patientId, DateOnly from, DateOnly to);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>The (order, slot) pairs already recorded for these orders between
        /// the two times, in every care home. For the missed-dose recorder only.</summary>
        Task<HashSet<(Guid OrderId, DateTime ScheduledFor)>> RecordedSlotsForAllCareHomesAsync(
            IReadOnlyCollection<Guid> orderIds, DateTime from, DateTime to, CancellationToken cancellationToken);

        /// <summary>Forgets unsaved additions, so a failed save is not retried with them.</summary>
        void DiscardPendingChanges();
    }
}