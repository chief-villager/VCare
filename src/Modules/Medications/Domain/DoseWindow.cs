namespace Medications.Domain
{
    /// <summary>
    /// The one rule for when a scheduled dose can be signed. The MAR chart,
    /// sign-off and the missed-dose recorder all ask this class, so they cannot
    /// disagree about whether a dose is due or missed.
    ///
    ///   before DueAt - EarlyAllowance          → not due yet: cannot be signed
    ///   DueAt - EarlyAllowance .. + GracePeriod → due: signed on time
    ///   after DueAt + GracePeriod              → missed, unless someone records what
    ///                                            happened; that entry needs a reason
    ///                                            and is flagged late
    ///   after DueAt + LateRecordingLimit       → can no longer be recorded here
    ///
    /// These are policy defaults, not clinical rules for every medicine: a
    /// time-critical one (levodopa, insulin) may need a tighter window. They
    /// belong to the home's medicines policy and should be agreed with its
    /// clinical lead.
    /// </summary>
    internal static class DoseWindow
    {
        // Lets the round start a little early. Without a lower bound, tonight's
        // dose (or tomorrow's) could be signed this morning: a double-dosing path.
        public static readonly TimeSpan EarlyAllowance = TimeSpan.FromHours(1);

        public static readonly TimeSpan GracePeriod = TimeSpan.FromHours(1);

        // A late entry is still the truth and must be recordable: a dose given at
        // 09:10 and signed after handover is not a missed dose. But records made
        // long after the event are how gaps get papered over before an
        // inspection, so after this the slot stays missed and is a matter for the
        // person in charge.
        public static readonly TimeSpan LateRecordingLimit = TimeSpan.FromHours(12);

        public static DateTime OpensAt(DateTime dueAt) => dueAt - EarlyAllowance;

        public static DateTime ClosesAt(DateTime dueAt) => dueAt + GracePeriod;

        public static DateTime RecordingClosesAt(DateTime dueAt) => dueAt + LateRecordingLimit;

        public static bool HasOpened(DateTime dueAt, DateTime now) => now >= OpensAt(dueAt);

        public static bool IsDue(DateTime dueAt, DateTime now) => HasOpened(dueAt, now) && !HasClosed(dueAt, now);

        public static bool HasClosed(DateTime dueAt, DateTime now) => now > ClosesAt(dueAt);

        public static bool CanStillBeRecorded(DateTime dueAt, DateTime now) => now <= RecordingClosesAt(dueAt);

        /// <summary>
        /// True when the dose's on-time window had already closed when the order
        /// was entered into VCare. Such a dose was never VCare's to miss: it
        /// belongs to whatever record the home kept before.
        /// </summary>
        public static bool ClosedBeforeOrderEntered(DateTime dueAt, DateTime orderEnteredAt) =>
            ClosesAt(dueAt) < orderEnteredAt;
    }
}
