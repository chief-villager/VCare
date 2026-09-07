using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Application.Services;

internal sealed class CarePlanService(IPatientRepository patientRepository, IUnitOfWork unitOfWork) : ICarePlanService
{
    public async Task<Result<Guid>> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken token)
    {
        var patient = await patientRepository.GetByIdAsync(request.PatientId, token);
        if (patient is null)
            return Result.Failure<Guid>("Patient not found.");
       

        var carePlan = patient.AddCarePlan(
            request.StaffId,
            request.Diagnoses,
            request.Goals,
            request.Interventions?.Select(i => (i.Description, i.Implemented)));

        if (carePlan.IsFailure)
            return Result.Failure<Guid>(carePlan.Error);

        await unitOfWork.SaveChangesAsync(token);
        return Result.Success(carePlan.Value.Id.Value);
    }

    public async Task<Result> UpdateCarePlanAsync(Guid carePlanId, UpdateCarePlanRequest request, CancellationToken token)
    {
        var id = new CarePlanId(carePlanId);
        var patient = await patientRepository.GetByCarePlanIdAsync(id, token);
        if (patient is null)
            return Result.Failure("Care plan not found.");
        
        var result = patient.UpdateCarePlan(
            id,
            request.Diagnoses,
            request.Goals,
            request.Interventions?.Select(i => (i.Description, i.Implemented)));

        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(token);
        return Result.Success();
    }
}
