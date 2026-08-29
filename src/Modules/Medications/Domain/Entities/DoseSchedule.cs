using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Enum;

namespace Medications.Domain.Entities
{
    internal class DoseSchedule
    {
        internal Guid Id { get; set; }
        internal Guid MedicationOrderId { get; set; }

        internal FrequencyType Type { get; set; }

        // The times of day a dose is due (e.g. 08:00, 14:00, 22:00)
        internal ICollection<TimeOnly> Times { get; set; } = [];

        // For interval patterns: "every N days"
        internal int IntervalDays { get; set; } = 1;

        // For weekly patterns: which days (Mon/Wed/Fri)
        internal DayOfWeekFlags DaysOfWeek { get; set; }

        // Anchor for interval maths (alternate-day needs a reference point)
        internal DateOnly AnchorDate { get; set; }
    }
}