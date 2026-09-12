using VCare.Modules.CarePlans.Application.Services;
using VCare.Modules.Patients.Application.Dtos;

namespace VCare.Api.Dtos;

// Spans two modules — the patient from Patients, the plan from CarePlans — so
// it belongs to the host that composes them, not to either module.
// CarePlan is null when the patient has no care plan yet; that is not a failure.
public sealed record PatientDashboardResponse(PatientResponse Patient, CarePlanResponse? CarePlan);
