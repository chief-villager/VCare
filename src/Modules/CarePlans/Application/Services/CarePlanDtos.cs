namespace VCare.Modules.CarePlans.Application.Services;

// The patient comes from the route, so a body can't create a plan for someone else.
public sealed record CreateCarePlanRequest(
    Guid StaffId,
    string Goal,
    List<DiagnosisInput>? Diagnoses);

public sealed record UpdateCarePlanRequest(
    List<DiagnosisInput>? Diagnoses, string? Goal);

public sealed record DiagnosisInput(string Name, string? GoalDescription, string? Intervention);

public sealed record CarePlanInterventionInput(string Description, bool Implemented);

public sealed record CarePlanResponse(
    Guid Id,
    Guid PatientId,
    string Goal,
    Guid StaffId,
    List<DiagnosisResponse> Diagnoses,
    DateOnly CreatedDate,
    DateOnly ModifiedDate);

public sealed record CarePlanInterventionResponse(string Description, bool Implemented);
public sealed record PatientGoalsResponse(string Description);
public sealed record DiagnosisResponse(string Name, string? GoalDescription, string? Intervention);
