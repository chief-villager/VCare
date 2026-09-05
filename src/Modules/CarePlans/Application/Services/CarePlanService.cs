using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarePlans.Application.Abstract;
using CarePlans.Application.Services.Interfaces;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Application.Services
{
    internal sealed class CarePlanService(ICarePlanRepository carePlanRepository, IUnitOfWork unitOfWork) : ICarePlanService
    {
        public async Task<Result<Guid>> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken token)
        {
            var carePlan = CarePlan.Create(
                request.PatientId,
                request.StaffId,
                request.Diagnoses,
                request.Goals,
                request.Interventions?.Select(i => (i.Description, i.Implemented)));

            if (carePlan.IsFailure)
                return Result.Failure<Guid>(carePlan.Error);

            await carePlanRepository.CreateCarePlanAsync(carePlan.Value, token);
            await unitOfWork.SaveChangesAsync(token);
            return Result.Success(carePlan.Value.Id.Value);
        }

        public async Task<Result> UpdateCarePlanAsync(Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token)
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

        public async Task<Result<CarePlanResponse>> GetCarePlanAsync(Guid patientId, CancellationToken token)
        {
            var carePlan = await carePlanRepository.GetCarePlanAync(patientId, token);
            return Result.Success(new CarePlanResponse(
                carePlan.Id.Value,
                carePlan.PatientId.Value,
                carePlan.StaffId,
                carePlan.Diagnoses.Select(d => new DiagnosisResponse(d.Description)).ToList(),
                carePlan.Goals.Select(g => new PatientGoalsResponse(g.GoalDescription)).ToList(),
                carePlan.Intervention.Select(i => new CarePlanInterventionResponse(i.Description, i.Implementation)).ToList(),
                carePlan.CreatedDate,
                carePlan.ModifiedDate
            ));
        }
    }
}
