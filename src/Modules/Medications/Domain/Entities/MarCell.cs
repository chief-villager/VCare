using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal class MarCell
    {
        public DateTime DueAt { get; set; }
        public MedicationAdministration? Administration { get; set; }    // null = nothing signed
    
        public bool IsSigned => Administration is not null;
        public bool IsMissed => !IsSigned && DueAt < DateTime.Now;    // past + unsigned = missed
        // (unsigned + future = neither: simply "not due yet")

        public MarCell(){}

      
    }

}