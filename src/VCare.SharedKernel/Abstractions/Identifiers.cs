using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VCare.SharedKernel.Abstractions
{
    public readonly record struct PatientId(Guid Value)
    {
        public static  PatientId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
        
    }

    public readonly record struct CarePlanId(Guid Value)
    {
        public static  CarePlanId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
        
    }

    public readonly record struct MedicationOrderId(Guid Value)
    {
        public static  MedicationOrderId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
        
    }
  
}