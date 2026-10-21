using VCare.SharedKernel.Results;

namespace Medications.Domain.Enum
{
    [Flags]
    public enum DayOfWeekFlags
    {
        None = 0, Mon = 1, Tue = 2, Wed = 4, Thu = 8,
        Fri = 16, Sat = 32, Sun = 64
    }


    public static class DaysOfWeekEnumChecker
    {
        // Built from the enum itself. Days are flags, so a schedule can name several,
        // comma-separated ("Mon, Thu"); each part must be an exact day name, which
        // rejects numbers ("5"). Empty means no specific days (None). System.Enum is
        // spelled out because this namespace is itself called Enum.
        private static readonly Dictionary<string, DayOfWeekFlags> Days =
            System.Enum.GetValues<DayOfWeekFlags>()
                .ToDictionary(r => r.ToString(), StringComparer.OrdinalIgnoreCase);

        public static Result<DayOfWeekFlags> DaysOfWeekChecker(string? days)
        {
            if (string.IsNullOrWhiteSpace(days))
                return Result.Success(DayOfWeekFlags.None);

            var combined = DayOfWeekFlags.None;
            foreach (var part in days.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Days.TryGetValue(part, out var day))
                    return Result.Failure<DayOfWeekFlags>(new Error("400",
                        $"Invalid day of week '{part}'. Expected comma-separated days from: {string.Join(", ", Days.Keys)}.",
                        ErrorKind.Validation));
                combined |= day;
            }
            return Result.Success(combined);
        }
    }

}