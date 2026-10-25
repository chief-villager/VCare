using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;
using Visitation.Domain.Entities;

namespace VCare.Modules.Visitation.Domain.Entities;

// One staff member's attendance at a visit. Created at check-in and closed at
// check-out, when the care given is recorded. Only reached through its Visit.
internal sealed class VisitRecord
{
    public Guid Id {get; private set;}
    public VisitId VisitId {get; private set;}
    public StaffId StaffId {get; private set;}
    public VisitStatus Status { get; private set; }
    public FeedingTask? FeedingTask {get; private set;}
    public MedicationTask? MedicationTask {get; private set;}
    public PersonalcareTask? PersonalcareTask {get; private set;}
    public DateTime CheckedInAt { get; private set; }
    public DateTime? CheckedOutAt { get; private set; }
    public string? VisitationSummary {get; private set;}
    private VisitRecord() { }

    // Times are stored in UTC, so check-in and check-out compare on the same clock.
    internal static Result<VisitRecord> CreateVisitRecord(VisitId visitId, StaffId staffId, DateTime checkedInAt)
    {
        if (staffId.Value == Guid.Empty)
            return Result.Failure<VisitRecord>("Staff member is required.");

        var visitRecord = new VisitRecord
        {
            Id = Guid.NewGuid(),
            VisitId = visitId,
            StaffId = staffId,
            Status = VisitStatus.CheckedIn,
            CheckedInAt = checkedInAt.ToUniversalTime(),
        };
        return Result.Success(visitRecord);
    }

    internal Result CheckOut(bool isMedicationCompleted, bool isPersonalCareCompleted, bool isFeedingTaskCompleted,
    string medicationTaskNote, string personalCareTaskNote, string feedingTaskNote, DateTime? checkedOutAt = null)
    {
        if (Status is not VisitStatus.CheckedIn)
            return Result.Failure(Error.Conflict("Visits.NotCheckedIn", $"Only a checked-in visit can be checked out; this one is {Status}."));
        if (string.IsNullOrWhiteSpace(medicationTaskNote))
            return Result.Failure("Medication task note is required.");
        if (string.IsNullOrWhiteSpace(personalCareTaskNote))
            return Result.Failure("Personal care task note is required.");
        if (string.IsNullOrWhiteSpace(feedingTaskNote))
            return Result.Failure("Feeding task note is required.");

        var leftAt = (checkedOutAt ?? DateTime.UtcNow).ToUniversalTime();
        if (leftAt < CheckedInAt)
            return Result.Failure("A visit cannot end before it began.");

        CheckedOutAt = leftAt;
        Status = VisitStatus.Completed;
        MedicationTask = MedicationTask.Create(Id, isMedicationCompleted, medicationTaskNote.Trim());
        FeedingTask = FeedingTask.Create(Id, isFeedingTaskCompleted, feedingTaskNote.Trim());
        PersonalcareTask = PersonalcareTask.Create(Id, isPersonalCareCompleted, personalCareTaskNote.Trim());
        return Result.Success();
    }
}
