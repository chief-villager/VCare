using Patients.Application.Dtos;
using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Application.Services;

internal  class PatientService(
    IPatientRepository repository, IUnitOfWork unitOfWork) : IPatientService
{
    public async Task<PatientResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        return patient is null
            ? null
            : new PatientResponse(patient.Id.Value, patient.FullName, patient.DateOfBirth);
    }

    public async Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = Patient.Register(request.firstname, request.lastname, 
        request.gender, request.address, request.dateOfBirth, 
        request.emergencyContactRelationship, request.emergencyContactPhoneNumber, 
        request.emergencyContactName, request.phoneNumber, request.email);

        await repository.AddAsync(patient.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PatientResponse>.Success(new PatientResponse(patient.Value.Id.Value, patient.Value.FullName, patient.Value.DateOfBirth));
    }

    public async Task<Result> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result.Failure("Patient not found");
        }

        patient.Update(request.Firstname, request.Lastname, request.Gender, request.Address, request.DateOfBirth, 
        request.EmergencyContactRelationship, request.EmergencyContactPhoneNumber, request.EmergencyContactName, 
        request.phoneNumber, request.Email);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();

    }
}
