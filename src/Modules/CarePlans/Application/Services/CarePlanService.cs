using CarePlans.Application.Services.Interfaces;
using VCare.Modules.CarePlans.Application.Abstractions;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Application.Services;

internal sealed class CarePlanService(ICarePlanRepository carePlanRepository, ICurrentUser currentUser) : ICarePlanService
{
    public async Task<Result<Guid>> CreateCarePlanAsync(Guid patientId, CreateCarePlanRequest request, CancellationToken token)
    {
        // One care plan per patient; the unique index on PatientId enforces it in the
        // database, this turns it into a 409 instead of a DbUpdateException.
        if (await carePlanRepository.ExistsForPatientAsync(new PatientId(patientId), token))
            return Result.Failure<Guid>(Error.Conflict(
                "CarePlans.AlreadyExists", "This patient already has a care plan. Update it instead."));

        // A care plan belongs to the care home of the caller creating it.
        var carePlan = CarePlan.Create(
            patientId,
            request.Goal,
            request.StaffId,
            currentUser.CareHomeId,
            request.Diagnoses?.Select(d => (d.Name, d.GoalDescription, d.Intervention)));

        if (carePlan.IsFailure)
            return Result.Failure<Guid>(carePlan.Error);

        await carePlanRepository.AddAsync(carePlan.Value, token);
        await carePlanRepository.SaveChangesAsync(token);
        return Result.Success(carePlan.Value.Id.Value);
    }

    public async Task<Result> UpdateCarePlanAsync(Guid patientId, Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token)
    {
        var carePlan = await carePlanRepository.GetByIdAsync(new CarePlanId(carePlanId), token);
        // A plan reached through another patient's URL is treated as not found.
        if (carePlan is null || carePlan.PatientId.Value != patientId)
            return Result.Failure(Error.NotFound("CarePlans.NotFound", "Care plan not found."));

        var result = carePlan.Update(
            request.Diagnoses?.Select(d => (d.Name, d.GoalDescription, d.Intervention)),
            request.Goal);

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
            carePlan.Goal,
            carePlan.StaffId,
            carePlan.Diagnoses.Select(d => new DiagnosisResponse(d.Name, d.GoalDescription, d.Intervention)).ToList(),
            carePlan.CreatedDate,
            carePlan.ModifiedDate));
    }
}
