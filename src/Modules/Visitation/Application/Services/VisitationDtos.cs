namespace VCare.Modules.Visitation.Application.Services;

// One visit is created per day, all at the same time of day.
public sealed record ScheduleVisitRequest(List<DateOnly> VisitDays, TimeOnly VisitTime);

public sealed record AssignStaffRequest(Guid StaffId);

// Check-in and check-out are timed by the server when the carer taps, never by a
// time the client sends: a visit record is evidence that someone was there, and
// a client-chosen time made it say whatever the client liked. Check-in has no body.
public sealed record CheckOutVisitRequest(bool IsMedicationCompleted, bool IsPersonalCareCompleted, bool IsFeedingTaskCompleted,
    string MedicationTaskNote, string PersonalCareTaskNote, string FeedingTaskNote);

public sealed record CancelVisitRequest(string Reason);

public sealed record VisitResponse(
    Guid Id,
    Guid PatientId,
    DateOnly VisitDay,
    TimeOnly VisitTime,
    bool IsCancelled,
    string? CancellationReason,
    List<Guid> StaffIds,
    List<VisitRecordResponse> Records);

// One staff member's attendance at a visit, from check-in to check-out.
public sealed record VisitRecordResponse(
    Guid Id,
    Guid StaffId,
    string Status,
    List<FeedingTask> Feeding,
    List<MedicationTask> Medication,
    List<PersonalCareTask> PersonalCare,
    DateTime CheckedInAt,
    DateTime? CheckedOutAt);

public sealed record FeedingTask(string Note, bool IsCompleted);
public sealed record MedicationTask(string Note, bool IsCompleted);

public sealed record PersonalCareTask(string Note, bool IsCompleted);
