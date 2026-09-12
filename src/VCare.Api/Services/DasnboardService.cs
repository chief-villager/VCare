using CarePlans.Application.Services.Interfaces;
using Patients.Application.Services.Interfaces;
using VCare.Api.Dtos;
using VCare.SharedKernel.Results;

namespace VCare.Api.Services
{
    // The patient and the care plan are owned by separate modules, so the
    // dashboard is composed here from two reads rather than one aggregate load.
    public class DashboardService(IPatientService patientService, ICarePlanService carePlanService)
    {
        public async Task<Result<PatientDashboardResponse>> PatientDashboardInformation(Guid patientId, CancellationToken token)
        {
            var patient = await patientService.GetByIdAsync(patientId, token);
            if (patient is null)
                return Result.Failure<PatientDashboardResponse>("Patient not found.");

            // An absent care plan is not an error.
            var carePlan = await carePlanService.GetForPatientAsync(patientId, token);
            if (carePlan.IsFailure)
                return Result.Failure<PatientDashboardResponse>(carePlan.Error);

            return Result.Success(new PatientDashboardResponse(patient, carePlan.Value));
        }
    }
}
