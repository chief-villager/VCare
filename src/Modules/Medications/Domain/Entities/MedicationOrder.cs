using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Enum;

namespace Medications.Domain.Entities
{
    internal sealed class MedicationOrder
    {
        internal Guid Id { get; set; }
        internal Guid PatientId { get; set; }
        internal string Medication { get; set; } = default!; 
        internal string Dose { get; set; } = default!;         // "1 tablet", "5ml"
        internal string Route { get; set; } = default!;        // oral, topical, s/c...
        internal string Instructions { get; set; } = string.Empty; // "take with food"
        internal DateOnly StartDate { get; set; }
        internal DateOnly? EndDate { get; set; }  // null = ongoing
        internal string Prescriber { get; set; } = default!;
        internal bool IsPrn { get; set; }          // "as required"
        internal string? PrnIndication { get; set; }   // "for pain"
        internal int? PrnMinIntervalMinutes { get; set; }
        internal string? PrnMaxDose24h { get; set; }
        internal bool IsControlledDrug { get; set; }
        internal OrderStatus Status { get; set; } 
        public ICollection<MedicationAdministration> Administrations { get; set; } = [];
        public ICollection<DoseSchedule> Schedule { get; set; }= [];







        private MedicationOrder()
        {
            
        }

        private MedicationOrder(string instruction)
        {
            
        }

    }
}