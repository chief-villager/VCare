using VCare.Modules.CarePlans.Application.Services;
using VCare.SharedKernel.Results;

namespace CarePlans.Application.Services.Interfaces;

public interface ICarePlanService
{
    Task<Result<Guid>> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken token);
    Task<Result> UpdateCarePlanAsync(Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token);

    // The patient's care plan, or null when they have none yet. An absent plan
    // is not a failure.
    Task<Result<CarePlanResponse?>> GetForPatientAsync(Guid patientId, CancellationToken token);
}
