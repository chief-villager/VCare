using VCare.Modules.Patients.Application.Services;
using VCare.SharedKernel.Results;

namespace Patients.Application.Services.Interfaces;

public interface ICarePlanService
{
    Task<Result<Guid>> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken token);
    Task<Result> UpdateCarePlanAsync(Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token);
}
