using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Enum;

namespace Medications.Domain.Entities
{
    internal class MedicationAdministrationOutcome
    {
        internal Guid Id {get; set;}
        internal OutcomeEnum Outcome {get; set;}
    }
}