using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Medications.Domain.Entities
{
    internal class MarRow
    {
        public MedicationOrder Order { get; set; } = null!;         // one order = one row
        public IReadOnlyList<MarCell> Cells { get; set; } = [];

        public MarRow(){}

       
    }
}