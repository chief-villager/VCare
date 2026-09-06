using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Dtos;
using VCare.SharedKernel.Results;

namespace VCare.Api.Services
{
    public class DashboardService(IPatientService patientService)
    {
        // A single aggregate read returns the patient together with its care plan
        // (if any). An absent care plan is not an error.
        public Task<Result<PatientDashboardResponse>> PatientDashboardInformation(Guid patientId, CancellationToken token)
            => patientService.GetDashboardAsync(patientId, token);
    }
}
