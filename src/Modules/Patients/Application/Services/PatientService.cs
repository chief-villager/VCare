using Patients.Application.Dtos;
using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.Modules.Patients.Domain.Errors;
using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Application.Services;

internal  class PatientService(
    IPatientRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser) : IPatientService
{
    public async Task<PatientResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        return patient is null
            ? null
            : new PatientResponse(patient.Id.Value, patient.FullName, patient.DateOfBirth);
    }

    public async Task<IReadOnlyList<PatientResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var patients = await repository.ListAsync(cancellationToken);
        return patients
            .Select(p => new PatientResponse(p.Id.Value, p.FullName, p.DateOfBirth))
            .ToList();
    }

    public async Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        var careHomeId = currentUser.CareHomeId;
        var patient = Patient.Register(request.firstname, request.lastname, 
        request.gender, request.address, request.dateOfBirth, 
        request.emergencyContactRelationship, request.emergencyContactPhoneNumber, 
        request.emergencyContactName, request.phoneNumber,careHomeId, request.email);

        if (patient.IsFailure)
            return Result.Failure<PatientResponse>(patient.Error);

        await repository.AddAsync(patient.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PatientResponse(patient.Value.Id.Value, patient.Value.FullName, patient.Value.DateOfBirth));
    }

    public async Task<Result> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result.Failure(PatientErrors.NotFound(id));
        }

        patient.Update(
            firstname: request.Firstname,
            lastname: request.Lastname,
            gender: request.Gender,
            address: request.Address,
            dateOfBirth: request.DateOfBirth,
            phoneNumber: request.phoneNumber,
            email: request.Email,
            emergencyContactName: request.EmergencyContactName,
            emergencyContactPhoneNumber: request.EmergencyContactPhoneNumber,
            emergencyContactRelationship: request.EmergencyContactRelationship);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();

    }
}
