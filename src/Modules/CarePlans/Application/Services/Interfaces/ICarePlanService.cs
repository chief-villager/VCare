using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.Modules.CarePlans.Application.Services;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Results;

namespace CarePlans.Application.Services.Interfaces
{
    public interface ICarePlanService
    {
        Task<Result<Guid>> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken token);
        Task<Result> UpdateCarePlanAsync(Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token);
        Task<Result<CarePlanResponse>> GetCarePlanAsync(Guid patientId, CancellationToken token);
    };
}