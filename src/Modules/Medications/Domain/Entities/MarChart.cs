using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Medications.Domain.Entities
{
    internal class MarChart
    {
        
        public IReadOnlyList<MarRow> Rows { get; set; } = [];
    
    }
}