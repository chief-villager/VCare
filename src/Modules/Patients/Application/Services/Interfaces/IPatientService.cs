using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.Modules.Patients.Application.Dtos;
using VCare.SharedKernel.Results;

namespace Patients.Application.Services.Interfaces
{
    public interface IPatientService
    {
        Task<PatientResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<PatientDashboardResponse>> GetDashboardAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default);
    }
}