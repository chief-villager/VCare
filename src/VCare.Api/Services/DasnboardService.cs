using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Patients.Application.Services.Interfaces;
using Medications.Application.Services.Interfaces;
using VCare.SharedKernel.Results;
using CarePlans.Application.Services.Interfaces;

namespace VCare.Api.Services
{
    public class DashboardService(IPatientService patientService, ICarePlanService carePlanService)
    {
        public async Task<Result> PatientDashboardInformation(Guid PatientId, CancellationToken token)
        {
            var patientTask = patientService.GetByIdAsync(PatientId, token);
            var carePlanTask = carePlanService.GetCarePlanAsync(PatientId, token);

            return await Task.WhenAll(patientTask, carePlanTask)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        return Result.Failure("Failed to retrieve dashboard information.");
                    }

                    var patientResult = patientTask.Result;
                    var carePlanResult = carePlanTask.Result;

                    if (patientResult == null || carePlanResult.IsFailure)
                    {
                        return Result.Failure("Patient or care plan information not found.");
                    }

                    // Combine the results into a single response object
                    var dashboardInfo = new
                    {
                        Patient = patientResult,
                        CarePlan = carePlanResult.Value
                    };

                    return Result.Success(dashboardInfo);
                });
        }
    }
    
}
