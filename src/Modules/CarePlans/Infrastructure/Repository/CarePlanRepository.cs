using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarePlans.Application.Abstract;
using Vcare.Modules.CarePlans.Infrastructure.Persistence;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace CarePlans.Infrastructure.Repository
{
    internal class CarePlanRepository(CarePlanDbContext dbContext) : ICarePlanRepository
    {
        public async Task CreateCarePlanAsync(CarePlan carePlan, CancellationToken token)
        {
             await dbContext.CarePlans.AddAsync(carePlan, token);
        }

        public async Task<CarePlan> GetCarePlanAync(Guid Id, CancellationToken token)
        {
            return await dbContext.CarePlans.FindAsync([Id, token], cancellationToken: token) ??
            throw new NotFoundException("Careplan not found");
        }

        public void UpdateCarePlan(CarePlan carePlan)
        {
            dbContext.CarePlans.Update(carePlan);
        }
    }
}