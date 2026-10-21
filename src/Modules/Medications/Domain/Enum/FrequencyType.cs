using VCare.SharedKernel.Results;

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

    public static class FrequencyTypeEnumChecker
    {
        private static readonly Dictionary<string, FrequencyType> Frequency = 
                System.Enum.GetValues<FrequencyType>()
                .ToDictionary(r => r.ToString(), StringComparer.OrdinalIgnoreCase);
        public static Result<FrequencyType> FrequencyTypeChecker(string frequency)
        {
            if (!string.IsNullOrWhiteSpace(frequency) && Frequency.TryGetValue(frequency.Trim(), out var parsed))
                return Result.Success(parsed);

            return Result.Failure<FrequencyType>(
                new Error("400",
                    $"Invalid frequency type '{frequency}'. Expected one of: {string.Join(", ", Frequency.Keys)}.",
                    ErrorKind.Validation));
        }
    }
}