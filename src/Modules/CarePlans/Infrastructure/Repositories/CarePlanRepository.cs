using Microsoft.EntityFrameworkCore;
using VCare.Modules.CarePlans.Application.Abstractions;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.Modules.CarePlans.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Infrastructure.Repositories;

internal sealed class CarePlanRepository(CarePlanDbContext context) : ICarePlanRepository
{
    public async Task<CarePlan?> GetByIdAsync(CarePlanId id, CancellationToken cancellationToken = default) =>
        await context.CarePlans
            .Include(c => c.Diagnoses)
            .Include(c => c.Goals)
            .Include(c => c.Intervention)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<CarePlan?> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default) =>
        await context.CarePlans
            .Include(c => c.Diagnoses)
            .Include(c => c.Goals)
            .Include(c => c.Intervention)
            .FirstOrDefaultAsync(c => c.PatientId == patientId, cancellationToken);

    public async Task AddAsync(CarePlan carePlan, CancellationToken cancellationToken = default) =>
        await context.CarePlans.AddAsync(carePlan, cancellationToken);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);
}
