namespace VCare.Modules.CarePlans.Application.Services;

// The patient comes from the route, so a body can't create a plan for someone else;
// the author is the signed-in caller, so a body can't name someone else either.
public sealed record CreateCarePlanRequest(
    string Goal,
    List<DiagnosisInput>? Diagnoses);

/// <summary>Version is the version the edit was made to, as read with the plan.</summary>
public sealed record UpdateCarePlanRequest(
    List<DiagnosisInput>? Diagnoses, string? Goal, int Version);

public sealed record DiagnosisInput(string Name, string? GoalDescription, string? Intervention);

public sealed record CarePlanInterventionInput(string Description, bool Implemented);

public sealed record CarePlanResponse(
    Guid Id,
    Guid PatientId,
    string Goal,
    Guid StaffId,
    List<DiagnosisResponse> Diagnoses,
    DateOnly CreatedDate,
    DateOnly ModifiedDate,
    int Version);

/// <summary>EditedAt is UTC.</summary>
public sealed record CarePlanVersionSummary(int Number, Guid EditedByStaffId, DateTime EditedAt);

/// <summary>The plan as it stood at one version. EditedAt is UTC.</summary>
public sealed record CarePlanVersionResponse(int Number, string Goal, List<DiagnosisResponse> Diagnoses,
    Guid EditedByStaffId, DateTime EditedAt);

public sealed record CarePlanInterventionResponse(string Description, bool Implemented);
public sealed record PatientGoalsResponse(string Description);
public sealed record DiagnosisResponse(string Name, string? GoalDescription, string? Intervention);
