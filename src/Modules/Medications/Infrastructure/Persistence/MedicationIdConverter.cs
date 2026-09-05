using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace Medications.Infrastructure.Persistence
{
    public class MedicationIdConverter : ValueConverter<MedicationOrderId, Guid>
    {
        public MedicationIdConverter() : base( x => x.Value, x => new MedicationOrderId(x))
        {
        }
        
    }
}