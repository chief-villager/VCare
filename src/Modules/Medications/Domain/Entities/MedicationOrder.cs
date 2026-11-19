using Medications.Domain.Enum;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal sealed class MedicationOrder : AggregateRoot<MedicationOrderId>
    {
        public PatientId PatientId { get; private set; }
        public CareHomeId CareHomeId { get; private set; }
        public string Medication { get; private set; } = null!; 
        public MedicationRouteEnum Route { get; private set; }        // oral, topical, s/c...
        public string? Instructions { get; private set; }  // "take with food"
        public DateOnly StartDate { get; private set; }
        public DateOnly? EndDate { get; private set; }  // null = ongoing
        public string Prescriber { get; private set; } = null!;
        public bool IsPrn { get; private set; }          // "as required"
        public string? PrnIndication { get; private set; }   // "for pain"
        public int? PrnMinIntervalMinutes { get; private set; }
        public string? PrnMaxDose24h { get; private set; }

        // How many PRN doses may be given in any 24 hours. PrnMaxDose24h is the
        // prescriber's wording ("max 8 tablets"); this is the number the system enforces.
        public int? PrnMaxDosesPer24h { get; private set; }
        public bool IsControlledDrug { get; private set; }
        public OrderStatus Status { get; private set; } 
        public ICollection<MedicationAdministration> Administrations { get; private set; } = [];
        public ICollection<DoseSchedule> Schedule { get; private set; }= [];
        public DateTime CreatedAt { get; private set; } = DateTime.Now;
        public DateTime? ModifiedAt { get; private set; } = null;

        // When the order last left Active. The MAR chart keeps showing the doses
        // that were due before this, so stopping an order never erases its history.
        public DateTime? StatusChangedAt { get; private set; }


        private MedicationOrder(){}

        public static Result<MedicationOrder> Record(
            Guid patientId, Guid careHomeId, string medication, 
            MedicationRouteEnum route, string? instruction, DateOnly startDate,
            DateOnly? endDate, string prescriber, bool isPrn, string? prnIndication,
            int? prnMinIntervalMinutes, string? prnMaxDose24h,
            bool isControlledDrug, OrderStatus orderStatus,
            IEnumerable<(string Dose, FrequencyType FType, List<TimeOnly> Times, int IntervalDays, 
            DayOfWeekFlags DaysOfWeek, DateOnly? AnchorDate, DateOnly EffectiveFrom, 
            DateOnly? EffectiveTo, int Sequence)> doseSchedule, DateTime createdAt, DateTime? modifiedAt = null,
            int? prnMaxDosesPer24h = null)

        {
             if (patientId == Guid.Empty)
                return Result.Failure<MedicationOrder>("Patient is required.");

            if (careHomeId == Guid.Empty)
                return Result.Failure<MedicationOrder>("Care home is required.");

            if (string.IsNullOrWhiteSpace(medication))
                return Result.Failure<MedicationOrder>("Medication is required.");

            if (endDate.HasValue && endDate.Value < startDate)
                return Result.Failure<MedicationOrder>(
                    "End date cannot be before start date.");

            if (isPrn && string.IsNullOrWhiteSpace(prnIndication))
                return Result.Failure<MedicationOrder>(
                    "PRN indication is required for PRN medication.");

            // Zero or negative limits would read as "no limit" to nobody and as a
            // block on every dose to the code; refuse them rather than guess.
            if (prnMinIntervalMinutes is < 1)
                return Result.Failure<MedicationOrder>("The minimum interval between PRN doses must be at least one minute.");
            if (prnMaxDosesPer24h is < 1)
                return Result.Failure<MedicationOrder>("The maximum number of PRN doses in 24 hours must be at least one.");

            var order = new MedicationOrder
            {
                Id = MedicationOrderId.New(),
                PatientId = new PatientId(patientId),
                CareHomeId = new CareHomeId(careHomeId),
                Medication = medication,
                Route = route,
                Instructions = instruction,
                StartDate = startDate,
                EndDate = endDate,
                Prescriber = prescriber,
                IsPrn = isPrn ,
                PrnIndication = prnIndication,
                PrnMinIntervalMinutes = prnMinIntervalMinutes,
                PrnMaxDose24h = prnMaxDose24h,
                PrnMaxDosesPer24h = prnMaxDosesPer24h,
                IsControlledDrug = isControlledDrug,
                Status = orderStatus,
                CreatedAt = createdAt
            };
            if (modifiedAt.HasValue)
            {
                order.ModifiedAt = modifiedAt.Value;
            }
        
            var schedules = order.ReplaceDetails(doseSchedule);
            if (schedules.IsFailure)
                return Result.Failure<MedicationOrder>(schedules.Error);

            // A regular order with nothing scheduled never falls due: the medicine
            // would silently never be given. A PRN order's doses are never
            // expanded, so a timed schedule on one would never fall due either.
            if (!isPrn && order.Schedule.Count == 0)
                return Result.Failure<MedicationOrder>("A regular order needs at least one dose schedule.");
            if (!isPrn && order.Schedule.Any(s => s.FType == FrequencyType.Prn))
                return Result.Failure<MedicationOrder>("The Prn frequency is only for as-required orders.");
            if (isPrn && order.Schedule.Any(s => s.FType != FrequencyType.Prn))
                return Result.Failure<MedicationOrder>("An as-required order cannot have timed doses. Record a separate regular order.");

            return Result.Success(order) ;
            
            
        }

        public Result UpdateStatus(string status, DateTime changedAt)
        {
            if( status == nameof(OrderStatus.Suspended) )
            {
                Status = OrderStatus.Suspended;
                StatusChangedAt = changedAt;
                ModifiedAt = changedAt;
                return Result.Success();
            }
            if ( status == nameof(OrderStatus.Discontinued))
            {
                Status = OrderStatus.Discontinued;
                StatusChangedAt = changedAt;
                ModifiedAt = changedAt;
                return Result.Success();
            }
            return Result.Failure("Invalid OrderStatus");
           
        }

        private Result ReplaceDetails( IEnumerable<(string Dose, FrequencyType FType, List<TimeOnly> Times, int IntervalDays,
            DayOfWeekFlags DaysOfWeek, DateOnly? AnchorDate, DateOnly EffectiveFrom,
            DateOnly? EffectiveTo, int Sequence)>? doseSchedule = null)
        {
            if (doseSchedule == null)
                return Result.Success();

            // Built in full before assigning, so one invalid schedule leaves none half-applied;
            // its failure is returned rather than read through .Value, which would throw.
            var schedules = new List<DoseSchedule>();
            foreach (var ds in doseSchedule)
            {
                var schedule = DoseSchedule.Create(Id.Value, ds.Dose, ds.FType, ds.Times, ds.IntervalDays,
                    ds.DaysOfWeek, ds.AnchorDate, ds.EffectiveFrom, ds.EffectiveTo, ds.Sequence);
                if (schedule.IsFailure)
                    return schedule;
                schedules.Add(schedule.Value);
            }
            Schedule = schedules;
            return Result.Success();
        }

    }
}