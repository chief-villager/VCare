using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Medications.Domain.Entities
{
    internal class MedicationAdministration
    {
        internal Guid Id { get; set; }
        internal Guid MedicationOrderId { get; set; }

        internal DateTime? ScheduledFor { get; set; }   // null for PRN
        internal DateTime? AdministeredAt { get; set; }
        internal Guid OutcomeCodeId { get; set; }       // Given, Refused, Omitted...
        internal Guid AdministeredByStaffId { get; set; }
        internal Guid? WitnessedByStaffId { get; set; } // controlled drugs
        internal string? QuantityGiven { get; set; }
        internal string? Notes { get; set; }
        internal DateTime CreatedAt { get; set; }
    }
}