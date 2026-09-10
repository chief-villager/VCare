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

    public readonly record struct StaffId(Guid Value)
    {
        public static  StaffId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
        
    }

    public readonly record struct MedicationAdministrationId(Guid Value)
    {
        public static  MedicationAdministrationId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
        
    }

      public readonly record struct CareHomeId(Guid Value)
    {
        public static  CareHomeId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
        
    }
  
}