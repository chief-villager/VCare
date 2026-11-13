using Medications.Application.Abstracts;
using Medications.Domain;
using Medications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medications.Application.Services
{
    /// <summary>
    /// Writes a "Missed" record for every scheduled dose whose window
    /// (<see cref="DoseWindow"/>) has closed with nothing recorded, across every
    /// care home. Without it, a dose nobody touched was missed only on screen:
    /// nothing in the database said so, so reports and audits never saw it.
    /// </summary>
    internal sealed class MissedDoseRecorder(
        IMedicationOrderRepository orders,
        IMedicalAdministrationRepository administrations,
        ScheduleExpander expander,
        ILogger<MissedDoseRecorder> logger)
    {
        // How far back each pass looks. Covers the app being down for a while;
        // a dose missed during a longer outage than this is not back-filled.
        internal static readonly TimeSpan Lookback = TimeSpan.FromDays(7);

        private const string Note = "Not signed within an hour of the due time; recorded automatically.";

        /// <returns>How many doses were recorded missed.</returns>
        public async Task<int> RecordMissedDosesAsync(DateTime now, CancellationToken cancellationToken)
        {
            var from = DateOnly.FromDateTime(now - Lookback);
            var to = DateOnly.FromDateTime(now);

            var live = await orders.ActiveScheduledForAllCareHomesAsync(from, to, cancellationToken);
            if (live.Count == 0)
            {
                return 0;
            }

            var recorded = await administrations.RecordedSlotsForAllCareHomesAsync(
                live.Select(o => o.Id.Value).ToList(),
                from.ToDateTime(TimeOnly.MinValue), now, cancellationToken);

            var missed = live
                .SelectMany(order => expander.ExpandSchedule(order, from, to)
                    .Where(slot => DoseWindow.HasClosed(slot.DueAt, now)
                                && !recorded.Contains((slot.OrderId, slot.DueAt)))
                    .Select(slot => MedicationAdministration.RecordMissed(
                        order, slot.DueAt, MedicationAdministration.SystemStaffId, Note)))
                .ToList();

            if (missed.Count == 0)
            {
                return 0;
            }

            foreach (var administration in missed)
            {
                await administrations.AddAsync(administration);
            }

            try
            {
                await administrations.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Most likely a carer signed one of these slots between the read and
                // the save, and the unique (order, slot) index refused the duplicate.
                // The whole batch rolls back; the next pass re-reads and skips it.
                administrations.DiscardPendingChanges();
                logger.LogWarning(ex, "Recording {Count} missed doses conflicted with another write; retrying next pass.", missed.Count);
                return 0;
            }

            logger.LogInformation("Recorded {Count} missed doses.", missed.Count);
            return missed.Count;
        }
    }
}
