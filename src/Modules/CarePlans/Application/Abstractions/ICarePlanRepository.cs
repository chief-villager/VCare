using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Application.Abstractions;

internal interface ICarePlanRepository
{
    Task<CarePlan?> GetByIdAsync(CarePlanId id, CancellationToken cancellationToken = default);
    Task<CarePlan?> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default);
    Task AddAsync(CarePlan carePlan, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
