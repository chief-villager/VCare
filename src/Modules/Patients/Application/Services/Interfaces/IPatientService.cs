using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Patients.Application.Dtos;
using VCare.Modules.Patients.Application.Dtos;
using VCare.SharedKernel.Results;

namespace Patients.Application.Services.Interfaces
{
    public interface IPatientService
    {
        Task<PatientResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PatientResponse>> ListAsync(CancellationToken cancellationToken = default);
        Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken = default);
    }
}
