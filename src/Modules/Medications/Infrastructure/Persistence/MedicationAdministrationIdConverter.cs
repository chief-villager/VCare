using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace Medications.Infrastructure.Persistence
{
    public class MedicationAdministrationIdConverter : ValueConverter<MedicationAdministrationId, Guid>
    {
        public MedicationAdministrationIdConverter() : base( x => x.Value, x => new MedicationAdministrationId(x))
        {
        }
        
    }
}