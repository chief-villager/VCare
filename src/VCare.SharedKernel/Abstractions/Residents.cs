namespace VCare.SharedKernel.Abstractions
{
    /// <summary>
    /// The wall-clock time in a care home. Dose times ("08:00") are times in the
    /// home, not on the server: on a server set to UTC, a home in a UTC+1 zone had
    /// every dose window an hour off. CareHomes owns each home's time zone.
    /// </summary>
    public interface ICareHomeClock
    {
        /// <summary>Now, as a clock on the wall in that care home reads it.</summary>
        Task<DateTime> NowAsync(Guid careHomeId, CancellationToken cancellationToken);
    }

    /// <summary>
    /// What a resident is allergic to, owned by the Patients module. Medications
    /// checks a new order against it.
    /// </summary>
    public interface IResidentAllergies
    {
        /// <summary>
        /// The substances recorded against the resident and not removed. Empty when
        /// none are recorded, which is not the same as "no known allergies".
        /// </summary>
        Task<IReadOnlyList<string>> ActiveSubstancesAsync(Guid patientId, CancellationToken cancellationToken);
    }

    public enum AbsenceKind
    {
        Hospital = 0,
        Leave = 1,
        /// <summary>Left the home for good. No dose is expected from then on, and none is recorded.</summary>
        Discharged = 2,
        /// <summary>Died. No dose is expected from then on, and none is recorded.</summary>
        Deceased = 3
    }

    /// <summary>
    /// A time the resident was away from the home. Times are wall-clock times in
    /// the home, like dose times. <see cref="EndedAt"/> is null while they are still away.
    /// </summary>
    public sealed record ResidentAbsence(Guid PatientId, AbsenceKind Kind, DateTime StartedAt, DateTime? EndedAt)
    {
        public bool Covers(DateTime at) => at >= StartedAt && (EndedAt is null || at < EndedAt);
    }

    /// <summary>
    /// When residents were away, owned by the Patients module. A dose due while a
    /// resident is in hospital or on leave was not missed by the home. A resident
    /// who was discharged or died has an absence that never ends.
    /// </summary>
    public interface IResidentAbsences
    {
        /// <summary>
        /// Absences of these residents that overlap [from, to], across care homes.
        /// Not filtered by the caller's care home, because the missed-dose recorder
        /// runs with no caller: pass only resident ids already read from your own
        /// care-home-scoped data.
        /// </summary>
        Task<IReadOnlyList<ResidentAbsence>> OverlappingAsync(IReadOnlyCollection<Guid> patientIds,
            DateTime from, DateTime to, CancellationToken cancellationToken);
    }
}
