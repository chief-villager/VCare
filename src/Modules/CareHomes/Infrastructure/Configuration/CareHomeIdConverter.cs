using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using src.Modules.CareHomes.Domain.Entity;
using VCare.SharedKernel.Abstractions;

namespace src.Modules.CareHomes.Infrastructure.Configuration
{
    internal sealed class CareHomeIdConverter : ValueConverter<CareHomeId, Guid>
    {
        public CareHomeIdConverter() : base( x => x.Value, x => new CareHomeId(x))
        {
        }
        
    }
}