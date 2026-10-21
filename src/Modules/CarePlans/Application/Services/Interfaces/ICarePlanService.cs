using VCare.Modules.CarePlans.Application.Services;
using VCare.SharedKernel.Results;

namespace CarePlans.Application.Services.Interfaces;

public interface ICarePlanService
{
    Task<Result<Guid>> CreateCarePlanAsync(Guid patientId, CreateCarePlanRequest request, CancellationToken token);
    Task<Result> UpdateCarePlanAsync(Guid patientId, Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token);

    // The patient's care plan, or null when they have none yet. An absent plan
    // is not a failure.
    Task<Result<CarePlanResponse?>> GetForPatientAsync(Guid patientId, CancellationToken token);
}
