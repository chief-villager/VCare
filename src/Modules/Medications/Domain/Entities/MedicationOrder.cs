using VCare.SharedKernel.Auditing;
using Medications.Domain.Enum;
using Medications.Domain.Errors;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal sealed class MedicationOrder : AggregateRoot<MedicationOrderId>, IAudited
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

        // When the status last changed. Kept alongside StatusHistory because orders
        // stopped before the history existed have only this to go on.
        public DateTime? StatusChangedAt { get; private set; }

        // Every hold, restart and stop, oldest first. Decides which planned doses
        // were expected (WasActiveAt), so a restart never turns the held days into misses.
        public ICollection<OrderStatusChange> StatusHistory { get; private set; } = [];

        // Who entered the order into VCare. Null on orders entered before this was kept.
        public Guid? CreatedByStaffId { get; private set; }

        // Set only when the medicine matched one of the resident's recorded allergies
        // and the person entering it confirmed with the prescriber that it should stand.
        public string? AllergyOverrideReason { get; private set; }
        public const int AllergyOverrideReasonMaxLength = 500;


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
            int? prnMaxDosesPer24h = null, Guid? createdByStaffId = null, string? allergyOverrideReason = null)

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
            if (allergyOverrideReason?.Trim().Length > AllergyOverrideReasonMaxLength)
                return Result.Failure<MedicationOrder>($"The allergy override reason can be at most {AllergyOverrideReasonMaxLength} characters.");

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
                CreatedAt = createdAt,
                CreatedByStaffId = createdByStaffId,
                AllergyOverrideReason = string.IsNullOrWhiteSpace(allergyOverrideReason) ? null : allergyOverrideReason.Trim()
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

        /// <summary>
        /// Holds, restarts or stops the order. A suspended order can be restarted;
        /// a discontinued one is finished, and a new prescription is a new order.
        /// </summary>
        public Result ChangeStatus(OrderStatus to, Guid staffId, string? reason, DateTime changedAt)
        {
            if (Status == OrderStatus.Discontinued)
                return Result.Failure(MedicationErrors.OrderDiscontinued);
            if (Status == to)
                return Result.Failure(MedicationErrors.StatusUnchanged(to.ToString()));
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(MedicationErrors.StatusReasonRequired);
            if (reason.Trim().Length > OrderStatusChange.ReasonMaxLength)
                return Result.Failure($"The reason can be at most {OrderStatusChange.ReasonMaxLength} characters.");
            if (staffId == Guid.Empty)
                return Result.Failure("StaffId id required");
            // Two changes at the same minute would make "which came first" ambiguous
            // for every dose between them.
            if (StatusHistory.Any(c => c.ChangedAt >= changedAt))
                return Result.Failure(Error.Conflict("Medications.StatusChangedConcurrently",
                    "The order's status was changed at the same time or later. Refresh and check."));

            StatusHistory.Add(OrderStatusChange.Create(Id, Status, to, changedAt, staffId, reason.Trim()));
            Status = to;
            StatusChangedAt = changedAt;
            ModifiedAt = changedAt;
            return Result.Success();
        }

        /// <summary>
        /// Whether a dose due at <paramref name="at"/> was expected: the order was
        /// active then. Doses due while it was held or after it stopped were not.
        /// </summary>
        public bool WasActiveAt(DateTime at)
        {
            if (StatusHistory.Count == 0)
            {
                // No recorded changes: either never changed, or stopped before the
                // history was kept, when only the time of the last change was stored.
                return Status == OrderStatus.Active || (StatusChangedAt is { } stoppedAt && at < stoppedAt);
            }
            var changes = StatusHistory.OrderBy(c => c.ChangedAt).ToList();
            var last = changes.LastOrDefault(c => c.ChangedAt <= at);
            var statusThen = last?.To ?? changes[0].From;
            return statusThen == OrderStatus.Active;
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