using CarePlans.Application.Services.Interfaces;
using VCare.Modules.CarePlans.Application.Abstractions;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Application.Services;

internal sealed class CarePlanService(ICarePlanRepository carePlanRepository, ICurrentUser currentUser, IPatientDirectory patients) : ICarePlanService
{
    public async Task<Result<Guid>> CreateCarePlanAsync(Guid patientId, CreateCarePlanRequest request, CancellationToken token)
    {
        // The patient comes from the URL and Patients owns it. Without this a plan
        // could be filed against a mistyped id, or against another home's resident;
        // and since the plan index is unique per patient, that would also block the
        // real home from ever creating theirs.
        if (!await patients.ExistsAsync(patientId, token))
            return Result.Failure<Guid>(Error.NotFound("CarePlans.PatientNotFound", "Resident not found."));

        // One care plan per patient; the unique index on PatientId enforces it in the
        // database, this turns it into a 409 instead of a DbUpdateException.
        if (await carePlanRepository.ExistsForPatientAsync(new PatientId(patientId), token))
            return Result.Failure<Guid>(Error.Conflict(
                "CarePlans.AlreadyExists", "This patient already has a care plan. Update it instead."));

        // A care plan belongs to the care home of the caller creating it.
        var carePlan = CarePlan.Create(
            patientId,
            request.Goal,
            currentUser.UserId,
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
            request.Goal, request.Version, currentUser.UserId, DateTime.UtcNow);

        if (result.IsFailure)
            return result;

        try
        {
            await carePlanRepository.SaveChangesAsync(token);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            when (ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                  || ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            // Another edit to the same version was saved between our read and this
            // save. Depending on which write reaches the database first, that shows
            // as the plan's version check or as the new version's number already taken.
            return Result.Failure(CarePlan.EditConflict(null));
        }
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
            carePlan.ModifiedDate,
            carePlan.Version));
    }

    public async Task<Result<IReadOnlyList<CarePlanVersionSummary>>> GetVersionsAsync(Guid patientId, CancellationToken token)
    {
        var carePlan = await carePlanRepository.GetForPatientAsync(new PatientId(patientId), token);
        if (carePlan is null)
            return Result.Failure<IReadOnlyList<CarePlanVersionSummary>>(NotFound);
        return Result.Success<IReadOnlyList<CarePlanVersionSummary>>(
            [.. carePlan.Versions.OrderBy(v => v.Number).Select(v => new CarePlanVersionSummary(v.Number, v.EditedByStaffId, v.EditedAt))]);
    }

    public async Task<Result<CarePlanVersionResponse>> GetVersionAsync(Guid patientId, int number, CancellationToken token)
    {
        var carePlan = await carePlanRepository.GetForPatientAsync(new PatientId(patientId), token);
        var version = carePlan?.Versions.FirstOrDefault(v => v.Number == number);
        if (version is null)
            return Result.Failure<CarePlanVersionResponse>(Error.NotFound("CarePlans.VersionNotFound", "That version of the care plan doesn't exist."));
        return Result.Success(new CarePlanVersionResponse(version.Number, version.Goal,
            [.. version.ReadDiagnoses().Select(d => new DiagnosisResponse(d.Name, d.GoalDescription, d.Intervention))],
            version.EditedByStaffId, version.EditedAt));
    }

    private static readonly Error NotFound = Error.NotFound("CarePlans.NotFound", "Care plan not found.");
}
