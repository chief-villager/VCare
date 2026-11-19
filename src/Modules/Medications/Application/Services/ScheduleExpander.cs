using Medications.Domain.Entities;
using Medications.Domain.Enum;

namespace Medications.Application.Services
{
    internal class ScheduleExpander
    {
        /// <summary>
        /// The doses an order expects now: none once it is suspended or
        /// discontinued. What sign-off, the due list and the missed-dose recorder use.
        /// </summary>
        public IEnumerable<DueSlot> ExpandSchedule(MedicationOrder order, DateOnly from, DateOnly to) =>
            order.Status == OrderStatus.Active ? ExpandPlanned(order, from, to) : [];

        /// <summary>
        /// The doses an order's plan names, whatever its status now. The MAR chart
        /// needs this: the doses that fell due before an order was stopped are part
        /// of the resident's record and must not vanish when it is.
        /// </summary>
        public IEnumerable<DueSlot> ExpandPlanned(MedicationOrder order, DateOnly from, DateOnly to)
        {
            if (order.IsPrn)
                yield break;

            var start = order.StartDate > from ? order.StartDate : from;
            var end = order.EndDate is { } e && e < to ? e : to;

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                // The phase(s) whose window covers this day.
                var activePhases = order.Schedule
                    .Where(s => day >= s.EffectiveFrom
                            && (s.EffectiveTo is null || day <= s.EffectiveTo))
                    .OrderBy(s => s.Sequence);

                foreach (var phase in activePhases)
                {
                    if (!IsDueOnDay(phase, day)) continue;

                    foreach (var time in phase.Times.Order())
                        yield return new DueSlot(
                            order.Id.Value, order.Medication, phase.Id, phase.Dose, day.ToDateTime(time));
                }
            }
        }


        private static bool IsDueOnDay(DoseSchedule s, DateOnly day)
        {
            return s.FType switch
            {
                FrequencyType.DailyAtTimes => true,
                // New schedules always carry an anchor; the fallback matches the domain default.
                FrequencyType.IntervalDays => (day.DayNumber - (s.AnchorDate ?? s.EffectiveFrom).DayNumber)
                                                    % s.IntervalDays == 0,
                FrequencyType.SpecificWeekdays => s.DaysOfWeek.HasFlag(ToFlag(day.DayOfWeek)),
                // A single dose, on the phase's first day. Before this it fell to the
                // default below and was never due: a stat dose that never appeared.
                FrequencyType.OneOff => day == s.EffectiveFrom,
                _ => false
            };
        }

        private static DayOfWeekFlags ToFlag(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => DayOfWeekFlags.Mon,   DayOfWeek.Tuesday   => DayOfWeekFlags.Tue,
            DayOfWeek.Wednesday => DayOfWeekFlags.Wed, DayOfWeek.Thursday => DayOfWeekFlags.Thu,
            DayOfWeek.Friday => DayOfWeekFlags.Fri,   DayOfWeek.Saturday  => DayOfWeekFlags.Sat,
            _ => DayOfWeekFlags.Sun
        };
    }
}
