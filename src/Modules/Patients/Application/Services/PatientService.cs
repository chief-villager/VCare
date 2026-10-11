using Patients.Application.Dtos;
using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.Modules.Patients.Domain.Errors;
using VCare.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using VCare.SharedKernel.Domain;

namespace VCare.Modules.Patients.Application.Services;

internal  class PatientService(
    IPatientRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser, IDocumentService documentService, IOptions<StorageEntity> options) : IPatientService
{
        readonly string BucketName = options.Value.BucketName;

        public  async Task<Result<string>> UploadPatientDocumentAsync(PatientId patientId, IFormFile formFile,string fileName, CancellationToken cancellationToken)
        {
            if(fileName == null || fileName.Trim().Length == 0)
            {
                return Result.Failure<string>(new Error("400", "File name is required", ErrorKind.Validation));
            }
            var patient = await repository.GetByIdAsync(patientId.Value, cancellationToken);
            if (patient is null)
            {
                return Result.Failure<string>(new Error("404", "Patient not found", ErrorKind.NotFound));
            }
            if(currentUser.CareHomeId != patient.CareHomeId.Value)
            {
                return Result.Failure<string>(new Error("403", "You are not authorized to upload documents for this patient", ErrorKind.Forbidden));
            }
            if (formFile.Length <= 0)
            {
                return Result.Failure<string>(new Error("400", "The uploaded file is empty.", ErrorKind.Validation));
            }
            var filePath = Path.GetFullPath(formFile.FileName);
            
            var key = await documentService.WritingAnObjectAsync(BucketName, filePath, currentUser.CareHomeId.ToString(), cancellationToken);
            if (key.IsFailure)
            {
                return Result.Failure<string>(key.Error);
            }
            var documentResult = patient.AddDocument(fileName, key.Value);
            if (documentResult.IsFailure)
            {
                return Result.Failure<string>(documentResult.Error);
            }
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success(key.Value);
        }
    
        public async Task<Result<(Stream Content, string ContentType)>> GetPatientDocumentsAsync(PatientId patientId, string key, CancellationToken cancellationToken)
        {
            var patient = await repository.GetByIdAsync(patientId.Value, cancellationToken);

            if (patient is null)
            {
                return Result.Failure<(Stream Content, string ContentType)>(new Error("404", "Patient not found", ErrorKind.NotFound));
            }

            if (currentUser.CareHomeId != patient.CareHomeId.Value)
            {
                return Result.Failure<(Stream Content, string ContentType)>(new Error("403", "You are not authorized to view documents for this patient", ErrorKind.Forbidden));
            }
            var document = await documentService.GetObjectAsync(BucketName, key, cancellationToken);
            return Result.Success(document);
        }

        public Task<Result<string>> UploadProfileImageForPatientAsync(PatientId patientId, IFormFile formFile, CancellationToken cancellationToken)
        {
            if (formFile.Length <= 0)
            {
                return Task.FromResult(Result.Failure<string>(new Error("400", "The uploaded file is empty.", ErrorKind.Validation)));
            }
            var patient = repository.GetByIdAsync(patientId.Value, cancellationToken).Result;
            if (patient is null)
            {
                return Task.FromResult(Result.Failure<string>(new Error("404", "Patient not found", ErrorKind.NotFound)));
            }
            if (currentUser.CareHomeId != patient.CareHomeId.Value)
            {
                return Task.FromResult(Result.Failure<string>(new Error("403", "You are not authorized to upload profile image for this patient", ErrorKind.Forbidden)));
            }
            var result = documentService.UploadProfileImageAsync(BucketName, formFile.FileName, currentUser.CareHomeId.ToString(), cancellationToken);
            patient.UpdateProfilePicture(result.Result.Value);
            repository.SaveChangesAsync(cancellationToken);
            return result;
            
        }



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
