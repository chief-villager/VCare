namespace Medications.Domain
{
    /// <summary>
    /// The one rule for when a scheduled dose can still be given. The MAR chart,
    /// sign-off and the missed-dose recorder all ask this class, so they cannot
    /// disagree about whether a dose is due or missed.
    ///
    ///   before DueAt                  → not due yet
    ///   DueAt .. DueAt + GracePeriod  → due (can still be signed)
    ///   after DueAt + GracePeriod     → missed
    /// </summary>
    internal static class DoseWindow
    {
        public static readonly TimeSpan GracePeriod = TimeSpan.FromHours(1);

        public static DateTime ClosesAt(DateTime dueAt) => dueAt + GracePeriod;

        public static bool IsDue(DateTime dueAt, DateTime now) => dueAt <= now && !HasClosed(dueAt, now);

        public static bool HasClosed(DateTime dueAt, DateTime now) => now > ClosesAt(dueAt);
    }
}
