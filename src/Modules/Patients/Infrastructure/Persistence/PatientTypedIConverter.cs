using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace Patients.Infrastructure.Persistence
{
    public class PatientTypedIConverter : ValueConverter<PatientId, Guid>
    {
        public PatientTypedIConverter() : base( x => x.Value, x => new PatientId(x))
        {
        }
    }
}