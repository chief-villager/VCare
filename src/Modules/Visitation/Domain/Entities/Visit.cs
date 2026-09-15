using System.Diagnostics.Eventing.Reader;
using VCare.Modules.Visitation.Domain.Events;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;
using Visitation.Domain.Entities;

namespace VCare.Modules.Visitation.Domain.Entities;

// An aggregate root of its own. It references the patient being visited by id
// only: the Patients module owns the patient, this module owns the visit.
internal sealed class Visit : AggregateRoot<VisitId>
{
    public PatientId PatientId { get; private set; }
    public CareHomeId CareHomeId { get; private set; }
    public StaffId StaffId { get; private set; }
    public VisitStatus Status { get; private set; }
    public FeedingTask? FeedingTask {get; private set;}
    public MedicationTask? MedicationTask {get; private set;}
    public PersonalcareTask? PersonalcareTask {get; private set;}
    public DateTime CheckedInAt { get; private set; }
    public DateTime? CheckedOutAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? VisitationSummary {get; private set;}
    private Visit() { }

    public static Result<Visit> CreateCheckin(
        Guid patientId,
        Guid careHomeId,
        Guid staffId,
        DateTime checkedinTime
        )
    {
        if (patientId == Guid.Empty)
            return Result.Failure<Visit>("Patient is required.");
         if (staffId == Guid.Empty)
            return Result.Failure<Visit>("Patient is required.");
        if (careHomeId == Guid.Empty)
            return Result.Failure<Visit>("carehomeid is required");

        var visit = new Visit
        {
            Id = VisitId.New(),
            PatientId = new PatientId(patientId),
            CareHomeId = new CareHomeId(careHomeId),
            StaffId = new StaffId(staffId),
            Status = VisitStatus.CheckedIn,
            CheckedInAt = checkedinTime.ToUniversalTime()

        };
        return Result.Success(visit);
    }

   
  

    public Result CheckOut(bool isMedicationCompleted, bool isPersonalCareCompleted, bool isFeedingTaskCompleted, 
    string medicationTaskNote, string personalCareTaskNote, string feedingTaskNote, DateTime? checkedOutAt = null)
    {
        if (Status is not VisitStatus.CheckedIn)
            return Result.Failure($"Only a checked-in visit can be checked out; this one is {Status}.");
        if (string.IsNullOrWhiteSpace(medicationTaskNote))
        {
            return Result.Failure("medication summary is required");
        }
        if (string.IsNullOrWhiteSpace(personalCareTaskNote))
        {
            return Result.Failure("medication summary is required");
        }
        if (string.IsNullOrWhiteSpace(feedingTaskNote))
        {
            return Result.Failure("medication summary is required");
        }
        

        var leftAt = checkedOutAt ?? DateTime.UtcNow;
        if (leftAt < CheckedInAt)
            return Result.Failure("A visit cannot end before it began.");

        CheckedOutAt = leftAt;
        Status = VisitStatus.Completed;
        MedicationTask = MedicationTask.Create(Id.Value, isMedicationCompleted, medicationTaskNote.Trim());
        FeedingTask = FeedingTask.Create(Id.Value, isFeedingTaskCompleted, feedingTaskNote.Trim());
        PersonalcareTask = PersonalcareTask.Create(Id.Value, isPersonalCareCompleted, personalCareTaskNote.Trim());

        Raise(new VisitorCheckedOut(Id.Value, PatientId.Value, leftAt));
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A cancellation reason is required.");
        if (Status is not VisitStatus.Scheduled)
            return Result.Failure($"Only a scheduled visit can be cancelled; this one is {Status}.");

        Status = VisitStatus.Cancelled;
        CancellationReason = reason.Trim();
       
        Raise(new VisitCancelled(Id.Value, PatientId.Value, CancellationReason));
        return Result.Success();
    }

    
}
