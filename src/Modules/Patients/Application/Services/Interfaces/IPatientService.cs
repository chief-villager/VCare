using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Patients.Application.Dtos;
using VCare.Modules.Patients.Application.Dtos;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Patients.Application.Services.Interfaces
{
    public interface IPatientService
    {
        Task<Result<(Stream Content, string ContentType)>> GetPatientDocumentsAsync(PatientId patientId, string key, CancellationToken cancellationToken);
        Task<Result<string>> UploadProfileImageForPatientAsync(PatientId patientId, IFormFile formFile, CancellationToken cancellationToken);
        Task<Result<string>> UploadPatientDocumentAsync(PatientId patientId, IFormFile formFile, string fileName, CancellationToken cancellationToken);
        Task<PatientResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PatientResponse>> ListAsync(CancellationToken cancellationToken = default);
        Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    }
}
