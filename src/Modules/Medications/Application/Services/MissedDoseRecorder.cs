using Medications.Application.Abstracts;
using Medications.Domain;
using Medications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VCare.SharedKernel.Abstractions;

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
        IResidentAbsences absences,
        ICareHomeClock homeClock,
        TimeProvider clock,
        ILogger<MissedDoseRecorder> logger)
    {
        // How far back each pass looks. Covers the app being down for a while;
        // a dose missed during a longer outage than this is not back-filled.
        internal static readonly TimeSpan Lookback = TimeSpan.FromDays(7);

        // Every care home's clock is within this of UTC (UTC-12 to UTC+14), so a
        // read this much wider than the lookback holds every home's window.
        private static readonly TimeSpan WidestOffset = TimeSpan.FromHours(14);

        private const string Note = "Not signed within an hour of the due time; recorded automatically.";

        /// <summary>
        /// One pass over every care home, each judged by the time on its own clock.
        /// A dose due while the resident was in hospital or on leave is recorded with
        /// that outcome instead of Missed: the home could not have given it.
        /// </summary>
        /// <returns>How many doses were recorded, missed or away.</returns>
        public async Task<int> RecordMissedDosesAsync(CancellationToken cancellationToken)
        {
            var utcNow = clock.GetUtcNow().UtcDateTime;
            var from = DateOnly.FromDateTime(utcNow - Lookback - WidestOffset);
            var to = DateOnly.FromDateTime(utcNow + WidestOffset);
            var start = from.ToDateTime(TimeOnly.MinValue);
            var end = to.ToDateTime(TimeOnly.MaxValue);

            var live = await orders.ActiveScheduledForAllCareHomesAsync(from, to, cancellationToken);
            if (live.Count == 0)
            {
                return 0;
            }

            var recorded = await administrations.RecordedSlotsForAllCareHomesAsync(
                live.Select(o => o.Id.Value).ToList(), start, end, cancellationToken);
            var away = await absences.OverlappingAsync(
                live.Select(o => o.PatientId.Value).Distinct().ToList(), start, end, cancellationToken);

            var missed = new List<MedicationAdministration>();
            foreach (var home in live.GroupBy(o => o.CareHomeId.Value))
            {
                var now = await homeClock.NowAsync(home.Key, cancellationToken);
                var homeFrom = DateOnly.FromDateTime(now - Lookback);
                var homeTo = DateOnly.FromDateTime(now);

                foreach (var order in home)
                {
                    var unrecorded = expander.ExpandSchedule(order, homeFrom, homeTo)
                        .Where(slot => DoseWindow.HasClosed(slot.DueAt, now)
                                    // A dose whose window closed before the order was entered
                                    // belongs to the home's earlier record, not to VCare: a
                                    // backdated admission order must not fill the MAR with misses.
                                    && !DoseWindow.ClosedBeforeOrderEntered(slot.DueAt, order.CreatedAt)
                                    && !recorded.Contains((slot.OrderId, slot.DueAt)));

                    foreach (var slot in unrecorded)
                    {
                        // Having left the home outweighs a hospital stay or leave that overlaps it.
                        var absence = away
                            .Where(a => a.PatientId == order.PatientId.Value && a.Covers(slot.DueAt))
                            .OrderByDescending(a => a.Kind is AbsenceKind.Discharged or AbsenceKind.Deceased)
                            .FirstOrDefault();
                        if (absence is null)
                        {
                            missed.Add(MedicationAdministration.RecordMissed(order, slot.DueAt, MedicationAdministration.SystemStaffId, Note, now));
                        }
                        else if (absence.Kind is AbsenceKind.Hospital or AbsenceKind.Leave)
                        {
                            missed.Add(MedicationAdministration.RecordAway(order, slot.DueAt,
                                absence.Kind == AbsenceKind.Hospital ? OutcomeCode.InHospitalId : OutcomeCode.OnLeaveId,
                                absence.Kind == AbsenceKind.Hospital
                                    ? "Resident in hospital; recorded automatically."
                                    : "Resident on leave; recorded automatically.",
                                now));
                        }
                        // Discharged or died: the dose was never the home's to give,
                        // so nothing is recorded for it.
                    }
                }
            }

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

            logger.LogInformation("Recorded {Count} doses as missed or away.", missed.Count);
            return missed.Count;
        }
    }
}
