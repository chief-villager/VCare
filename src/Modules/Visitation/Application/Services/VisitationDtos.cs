namespace VCare.Modules.Visitation.Application.Services;

public sealed record ScheduleVisitRequest(
    Guid PatientId,
    DateTime ScheduledFor,
    List<VisitorInput> Visitors,
    string? Purpose);

public sealed record RescheduleVisitRequest(
    DateTime? ScheduledFor,
    List<VisitorInput>? Visitors,
    string? Purpose);

public sealed record CheckInVisitRequest(DateTime CheckedInAt);

public sealed record CheckOutVisitRequest(DateTime? CheckedOutAt, bool IsMedicationCompleted, bool IsPersonalCareCompleted, bool IsFeedingTaskCompleted, 
    string MedicationTaskNote, string PersonalCareTaskNote, string FeedingTaskNote);

public sealed record CancelVisitRequest(string Reason);

public sealed record VisitorInput(string FullName, string Relationship, string? PhoneNumber);

public sealed record VisitResponse(
    Guid Id,
    Guid PatientId,
    Guid StaffId,
    string Status,
    List<FeedingTask> Feeding,
    List<MedicationTask> Medication,
    List<PersonalCareTask> PersonalCare,
    DateTime? CheckedInAt,
    DateTime? CheckedOutAt);

public sealed record FeedingTask(string Note, bool IsCompleted);
public sealed record MedicationTask(string Note, bool IsCompleted);

public sealed record PersonalCareTask(string Note, bool IsCompleted);

