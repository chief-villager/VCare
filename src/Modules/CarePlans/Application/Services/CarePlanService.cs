using CarePlans.Application.Services.Interfaces;
using VCare.Modules.CarePlans.Application.Abstractions;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Application.Services;

internal sealed class CarePlanService(ICarePlanRepository carePlanRepository, ICurrentUser currentUser) : ICarePlanService
{
    public async Task<Result<Guid>> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken token)
    {
        // A care plan belongs to the care home of the caller creating it.
        var carePlan = CarePlan.Create(
            request.PatientId,
            request.StaffId,
            currentUser.CareHomeId,
            request.Diagnoses,
            request.Goals,
            request.Interventions?.Select(i => (i.Description, i.Implemented)));

        if (carePlan.IsFailure)
            return Result.Failure<Guid>(carePlan.Error);

        await carePlanRepository.AddAsync(carePlan.Value, token);
        await carePlanRepository.SaveChangesAsync(token);
        return Result.Success(carePlan.Value.Id.Value);
    }

    public async Task<Result> UpdateCarePlanAsync(Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token)
    {
        var carePlan = await carePlanRepository.GetByIdAsync(new CarePlanId(carePlanId), token);
        if (carePlan is null)
            return Result.Failure("Care plan not found.");

        var result = carePlan.Update(
            request.Diagnoses,
            request.Goals,
            request.Interventions?.Select(i => (i.Description, i.Implemented)));

        if (result.IsFailure)
            return result;

        await carePlanRepository.SaveChangesAsync(token);
        return Result.Success();
    }

    public async Task<Result<CarePlanResponse?>> GetForPatientAsync(Guid patientId, CancellationToken token)
    {
        var carePlan = await carePlanRepository.GetForPatientAsync(new PatientId(patientId), token);
        if (carePlan is null)
            return Result.Success<CarePlanResponse?>(null);

        return Result.Success<CarePlanResponse?>(new CarePlanResponse(
            carePlan.Id.Value,
            carePlan.PatientId.Value,
            carePlan.StaffId,
            carePlan.Diagnoses.Select(d => new DiagnosisResponse(d.Description)).ToList(),
            carePlan.Goals.Select(g => new PatientGoalsResponse(g.GoalDescription)).ToList(),
            carePlan.Intervention.Select(i => new CarePlanInterventionResponse(i.Description, i.Implementation)).ToList(),
            carePlan.CreatedDate,
            carePlan.ModifiedDate));
    }
}
