using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarePlans.Domain.Entities
{
    public class Diagnosis
    {
        public Guid CarePlanId {get; private set;}
        public  string Description { get;  private set; } = string.Empty;
        private Diagnosis() {}
        private Diagnosis( Guid carePlanId, string description)
        {
            Description = description;
            CarePlanId = carePlanId;
        }

        public static Diagnosis Create(Guid carePlanId, string description) => new(carePlanId, description);
    }
}