using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Visitation.Domain.Entities
{
    // A visit booked for one patient on one day. Staff are assigned to it ahead of
    // time, and each assigned staff member who turns up gets a VisitRecord of their
    // own covering their check-in, check-out and the care they gave.
    internal class Visit : AggregateRoot<VisitId>
    {
        public PatientId PatientId { get; private set; }
        public DateOnly VisitDay {get; private set;}
        public CareHomeId CareHomeId { get; private set; }
        public TimeOnly VisitTime {get; private set;}
        public List<StaffId> StaffIds {get; private set;} = [];
        public List<VisitRecord>VisitRecords {get; private set;} = [];
        public string? CancellationReason { get; private set; }
        public bool IsCancelled => CancellationReason is not null;

        private Visit()
        {

        }

        // One visit per day, all at the same time of day.
        public static Result<List<Visit>> CreateVisit(
            PatientId patientId, CareHomeId careHomeId,List<DateOnly> visitDays, TimeOnly visitTime)
        {
            if (patientId.Value == Guid.Empty)
                return Result.Failure<List<Visit>>("Patient is required.");
            if (careHomeId.Value == Guid.Empty)
                return Result.Failure<List<Visit>>("Care home is required.");
            if (visitDays is null || visitDays.Count == 0)
                return Result.Failure<List<Visit>>(Error.Validation("Visits.NoDays", "At least one visit day is required."));

            var visits = visitDays.Distinct().Select(day => new Visit
            {
                Id = VisitId.New(),
                PatientId = patientId,
                CareHomeId = careHomeId,
                VisitDay = day,
                VisitTime = visitTime
            }).ToList();
            return Result.Success(visits);
        }

        public Result AssignVisit(StaffId staffId)
        {
            if (IsCancelled)
                return Result.Failure(VisitCancelledError);
            if (StaffIds.Contains(staffId))
                return Result.Failure(Error.Conflict("Visits.StaffAlreadyAssigned", "Staff member is already assigned to this visit."));

            StaffIds = [..StaffIds,staffId];
            return Result.Success();
        }

        // Only assigned staff can check in, and each of them once.
        public Result Checkin(DateTime checkinTime, StaffId staffId)
        {
            if (IsCancelled)
                return Result.Failure(VisitCancelledError);
            if (!StaffIds.Contains(staffId))
                return Result.Failure(Error.Forbidden("Visits.StaffNotAssigned", "Staff member is not assigned to this visit."));
            if (VisitRecords.Any(r => r.StaffId == staffId))
                return Result.Failure(Error.Conflict("Visits.AlreadyCheckedIn", "Staff member has already checked in to this visit."));

            var record = VisitRecord.CreateVisitRecord(Id, staffId, checkinTime);
            if (record.IsFailure)
                return Result.Failure(record.Error);

            VisitRecords.Add(record.Value);
            return Result.Success();
        }

        public Result CheckOut( StaffId staffId ,bool isMedicationCompleted, bool isPersonalCareCompleted, bool isFeedingTaskCompleted,
        string medicationTaskNote, string personalCareTaskNote, string feedingTaskNote, DateTime? checkedOutAt = null)
        {
            var record = VisitRecords.FirstOrDefault(r => r.StaffId == staffId);
            if (record is null)
                return Result.Failure(Error.Conflict("Visits.NotCheckedIn", "Staff member has not checked in to this visit."));

            return record.CheckOut(isMedicationCompleted, isPersonalCareCompleted, isFeedingTaskCompleted,
                medicationTaskNote, personalCareTaskNote, feedingTaskNote, checkedOutAt);
        }

        // A visit can only be called off before anyone has turned up for it.
        public Result Cancel(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure("A cancellation reason is required.");
            if (IsCancelled)
                return Result.Failure(VisitCancelledError);
            if (VisitRecords.Count > 0)
                return Result.Failure(Error.Conflict("Visits.AlreadyStarted", "A visit that staff have checked in to cannot be cancelled."));

            CancellationReason = reason.Trim();
            return Result.Success();
        }

        private static readonly Error VisitCancelledError =
            Error.Conflict("Visits.Cancelled", "This visit has been cancelled.");
    }
}
