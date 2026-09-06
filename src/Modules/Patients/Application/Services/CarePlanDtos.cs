namespace VCare.Modules.Patients.Application.Services;

public sealed record CreateCarePlanRequest(
    Guid PatientId,
    Guid StaffId,
    List<string>? Diagnoses,
    List<string>? Goals,
    List<CarePlanInterventionInput>? Interventions);

public sealed record UpdateCarePlanRequest(
    List<string>? Diagnoses,
    List<string>? Goals,
    List<CarePlanInterventionInput>? Interventions);

public sealed record CarePlanInterventionInput(string Description, bool Implemented);

public sealed record CarePlanResponse(
    Guid Id,
    Guid PatientId,
    Guid StaffId,
    List<DiagnosisResponse> Diagnoses,
    List<PatientGoalsResponse> Goals,
    List<CarePlanInterventionResponse> Interventions,
    DateOnly CreatedDate,
    DateOnly ModifiedDate);

public sealed record CarePlanInterventionResponse(string Description, bool Implemented);
public sealed record PatientGoalsResponse(string Description);
public sealed record DiagnosisResponse(string Description);
