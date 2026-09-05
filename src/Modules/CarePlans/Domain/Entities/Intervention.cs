using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;

namespace CarePlans.Domain.Entities
{
    public class Intervention
    {
        public CarePlanId CarePlanId {get; private set;}
        public string  Description {get; set;} = string.Empty;
        public bool Implementation {get; set;}

        public Intervention(){}

        public Intervention( CarePlanId carePlanId, string description, bool implementation)
        {
            Description = description;
            Implementation = implementation;
            CarePlanId = carePlanId;
        }
    }
}