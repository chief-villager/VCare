using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VCare.SharedKernel.Abstractions;

namespace CarePlans.Infrastructure.Persistence
{
    public class CarePlanTypedIdConverter : ValueConverter<CarePlanId, Guid>
    {
        public CarePlanTypedIdConverter() : base( x => x.Value, x => new CarePlanId(x))
        {
        }
    }
}