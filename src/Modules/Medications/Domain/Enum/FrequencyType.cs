namespace Medications.Domain.Enum
{
    public enum FrequencyType
    {
        DailyAtTimes,     // OD, BD, TDS, QDS — N fixed times every day
        IntervalDays,     // alternate days, every 3 days
        SpecificWeekdays, // "Mondays only", "Mon/Thu"
        Prn,              // as required — no schedule at all
        OneOff            // single dose (e.g. a stat dose)
    }
}