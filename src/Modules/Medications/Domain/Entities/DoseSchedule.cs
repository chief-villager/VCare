using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Medications.Domain.Enum;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal class DoseSchedule
    {
        public Guid Id { get; private set; }
        public Guid MedicationOrderId { get; private set; }
        public string Dose { get; private set; } = null!;         // "1 tablet", "5ml"

        public FrequencyType FType { get; private set; }

        // The times of day a dose is due (e.g. 08:00, 14:00, 22:00)
        public ICollection<TimeOnly> Times { get; private set; } = [];

        // For interval patterns: "every N days"
        public int IntervalDays { get; private set; } = 1;

        // For weekly patterns: which days (Mon/Wed/Fri)
        public DayOfWeekFlags DaysOfWeek { get; private set; }

        // Anchor for interval maths (alternate-day needs a reference point)
        public DateOnly? AnchorDate { get; private set; }

        public DateOnly EffectiveFrom { get; set; }   // NEW — this phase's window start
        public DateOnly? EffectiveTo { get; set; }     // NEW — null = open-ended
        public int Sequence { get; set; } 

        private DoseSchedule(){}

        private DoseSchedule(Guid medicationOrderId,string dose, FrequencyType fType, List<TimeOnly> times,
         int intervalDays, DayOfWeekFlags dayOfWeek, DateOnly? anchorDate, DateOnly effectiveFrom, DateOnly? effectiveTo, int sequence)
        {
            Id = Guid.NewGuid();
            Dose = dose;
            FType = fType;
            Times = times;
            IntervalDays = intervalDays;
            DaysOfWeek = dayOfWeek;
            AnchorDate = anchorDate;
            MedicationOrderId = medicationOrderId;
            EffectiveFrom = effectiveFrom;
            EffectiveTo = effectiveTo;
            Sequence = sequence;
        }

        public static Result<DoseSchedule> Create(Guid medicationOrderId, string dose, FrequencyType fType, List<TimeOnly> times,
         int intervalDays, DayOfWeekFlags dayOfWeek, DateOnly anchorDate , DateOnly effectiveFrom, DateOnly? effectiveTo, int sequence)
        {
            if (medicationOrderId == Guid.Empty)
            {
                return Result.Failure<DoseSchedule>("MedicationId is required");
            }
            if (string.IsNullOrWhiteSpace(dose))
            {
                return Result.Failure<DoseSchedule>("Dose is required");
            }
            var doseSchedule = new DoseSchedule(medicationOrderId,dose,fType, times, intervalDays, dayOfWeek, anchorDate, effectiveFrom, effectiveTo, sequence);
            return Result.Success(doseSchedule);
        }
        
        

    }
}