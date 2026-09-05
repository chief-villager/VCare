using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;

namespace CarePlans.Domain.Entities
{
    public class Diagnosis
    {
        public CarePlanId CarePlanId {get; private set;}
        public  string Description { get;  private set; } = string.Empty;
        private Diagnosis() {}
        private Diagnosis( CarePlanId carePlanId, string description)
        {
            Description = description;
            CarePlanId = carePlanId;
        }

        public static Diagnosis Create(CarePlanId carePlanId, string description) => new(carePlanId, description);
    }
}