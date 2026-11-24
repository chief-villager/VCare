using Medications.Domain.Entities;

namespace Medications.Application.Abstracts
{
    internal interface IMedicalAdministrationRepository
    {
        Task AddAsync(MedicationAdministration administration);
        Task<MedicationAdministration?> Find(Guid orderId, DateTime scheduledFor);

        /// <summary>A resident's administrations between the two days: scheduled ones
        /// by their slot, PRN ones (which have no slot) by when they were given.</summary>
        Task<List<MedicationAdministration>> ForResidentBetweenAsync(Guid patientId, DateOnly from, DateOnly to, CancellationToken cancellationToken);

        /// <summary>When PRN doses of this order were actually given (Taken or Vomited)
        /// between the two times. What the minimum interval and 24-hour limit count.</summary>
        Task<List<DateTime>> PrnGivenTimesAsync(Guid orderId, DateTime from, DateTime to, CancellationToken cancellationToken);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>The (order, slot) pairs already recorded for these orders between
        /// the two times, in every care home. For the missed-dose recorder only.</summary>
        Task<HashSet<(Guid OrderId, DateTime ScheduledFor)>> RecordedSlotsForAllCareHomesAsync(
            IReadOnlyCollection<Guid> orderIds, DateTime from, DateTime to, CancellationToken cancellationToken);

        /// <summary>Forgets unsaved additions, so a failed save is not retried with them.</summary>
        void DiscardPendingChanges();
    }
}
