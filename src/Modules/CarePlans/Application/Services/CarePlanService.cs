using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarePlans.Application.Abstract;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Application.Services
{
    internal sealed class CarePlanService(ICarePlanRepository carePlanRepository, IUnitOfWork unitOfWork)
    {
        public async Task<Result<CarePlan>> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken token)
        {
            var carePlan = CarePlan.Create(
                request.PatientId,
                request.StaffId,
                request.Diagnoses,
                request.Goals,
                request.Interventions?.Select(i => (i.Description, i.Implemented)));

            if (carePlan.IsFailure)
                return Result.Failure<CarePlan>(carePlan.Error);

            await carePlanRepository.CreateCarePlanAsync(carePlan.Value, token);
            await unitOfWork.SaveChangesAsync(token);
            return carePlan;
        }

        public async Task<Result<CarePlan>> UpdateCarePlanAsync(Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token)
        {
            var carePlan = await carePlanRepository.GetCarePlanAync(carePlanId, token);

            var result = carePlan.Update(
                request.Diagnoses,
                request.Goals,
                request.Interventions?.Select(i => (i.Description, i.Implemented)));

            if (result.IsFailure)
                return Result.Failure<CarePlan>(result.Error);

            carePlanRepository.UpdateCarePlan(carePlan);
            await unitOfWork.SaveChangesAsync(token);
            return Result.Success(carePlan);
        }

        public async Task<Result<CarePlan>> GetCarePlanAsync(Guid carePlanId, CancellationToken token)
        {
            var carePlan = await carePlanRepository.GetCarePlanAync(carePlanId, token);
            return Result.Success(carePlan);
        }
    }
}
