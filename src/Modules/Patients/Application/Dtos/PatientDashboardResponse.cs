using VCare.Modules.Patients.Application.Services;

namespace VCare.Modules.Patients.Application.Dtos;

// A patient plus its care plan (if any), loaded in a single aggregate read.
// CarePlan is null when the patient has no care plan yet — not a failure.
public sealed record PatientDashboardResponse(PatientResponse Patient, CarePlanResponse? CarePlan);
