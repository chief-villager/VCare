using System;
using System.Collections.Generic;

namespace VCare.Modules.CarePlans.Application.Services
{
    internal sealed record CreateCarePlanRequest(
        Guid PatientId,
        Guid StaffId,
        List<string>? Diagnoses,
        List<string>? Goals,
        List<CarePlanInterventionInput>? Interventions);

    internal sealed record UpdateCarePlanRequest(
        List<string>? Diagnoses,
        List<string>? Goals,
        List<CarePlanInterventionInput>? Interventions);

    internal sealed record CarePlanInterventionInput(string Description, bool Implemented);
}
