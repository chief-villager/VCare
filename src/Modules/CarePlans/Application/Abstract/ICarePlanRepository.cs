using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.Modules.CarePlans.Domain.Entities;

namespace CarePlans.Application.Abstract
{
    public interface ICarePlanRepository
    {
        Task<CarePlan> GetCarePlanAync(Guid Id, CancellationToken token);
        Task CreateCarePlanAsync(CarePlan carePlan, CancellationToken token );
        void UpdateCarePlan(CarePlan carePlan);
    }
}