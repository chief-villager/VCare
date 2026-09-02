using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;
using Medications.Domain.Enum;

namespace Medications.Application.Services
{
    internal class ScheduleExpander
    {
        public IEnumerable<DueSlot> ExpandSchedule(MedicationOrder order, DateOnly from, DateOnly to)
        {
            if (order.Status != OrderStatus.Active || order.IsPrn)
                yield break;

            var start = order.StartDate > from ? order.StartDate : from;
            var end = order.EndDate is { } e && e < to ? e : to;

            for (var day = start; day <= end; day = day.AddDays(1))
            {
                // NEW: find the phase(s) whose window covers this day
                var activePhases = order.Schedule
                    .Where(s => day >= s.EffectiveFrom
                            && (s.EffectiveTo is null || day <= s.EffectiveTo))
                    .OrderBy(s => s.Sequence);

                foreach (var phase in activePhases)
                {
                    if (!IsDueOnDay(phase, day)) continue;

                    foreach (var time in phase.Times)
                        yield return new DueSlot(
                            order.Id, phase.Id, phase.Dose, day.ToDateTime(time));
                }
            }
        }

        private bool IsDueOnDay(DoseSchedule s, DateOnly day)
        {
            return s.FType switch
            {
                FrequencyType.DailyAtTimes => true,
                FrequencyType.IntervalDays => (day.DayNumber - s.AnchorDate.GetValueOrDefault().DayNumber)
                                                    % s.IntervalDays == 0,
                FrequencyType.SpecificWeekdays => s.DaysOfWeek.HasFlag(ToFlag(day.DayOfWeek)),
                _ => false
            };
        }

        private DayOfWeekFlags ToFlag(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => DayOfWeekFlags.Mon,   DayOfWeek.Tuesday   => DayOfWeekFlags.Tue,
            DayOfWeek.Wednesday => DayOfWeekFlags.Wed, DayOfWeek.Thursday => DayOfWeekFlags.Thu,
            DayOfWeek.Friday => DayOfWeekFlags.Fri,   DayOfWeek.Saturday  => DayOfWeekFlags.Sat,
            _ => DayOfWeekFlags.Sun
        };
    }
}