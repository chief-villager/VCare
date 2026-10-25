namespace VCare.Modules.Visitation.Application.Services;

// One visit is created per day, all at the same time of day.
public sealed record ScheduleVisitRequest(List<DateOnly> VisitDays, TimeOnly VisitTime);

public sealed record AssignStaffRequest(Guid StaffId);

public sealed record CheckInVisitRequest(DateTime CheckedInAt);

public sealed record CheckOutVisitRequest(DateTime? CheckedOutAt, bool IsMedicationCompleted, bool IsPersonalCareCompleted, bool IsFeedingTaskCompleted,
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
