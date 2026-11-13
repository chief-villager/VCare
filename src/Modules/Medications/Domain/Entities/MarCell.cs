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
        // Carried from the slot: a phased schedule can change the dose part-way
        // through the month, so it belongs to the square, not the row.
        public string? Dose { get; set; }
        public MedicationAdministration? Administration { get; set; }    // null = nothing recorded
        // When the chart was built. Every cell of one chart judges against the same instant.
        public DateTime Now { get; set; } = DateTime.Now;

        // Missed is either stored (the recorder, or a late sign-off, wrote a
        // "Missed" row) or not yet stored (the window closed and the recorder has
        // not run since). Both read the same: DoseWindow decides, nothing else.
        public bool IsMissed => Administration is null
            ? DoseWindow.HasClosed(DueAt, Now)
            : Administration.IsMissed;

        // A carer recorded an outcome: Taken, Refused...
        public bool IsSigned => Administration is not null && !Administration.IsMissed;

        // Time has come, the grace period has not run out: still signable.
        public bool IsDue => Administration is null && DoseWindow.IsDue(DueAt, Now);
        // (none of the three = not due yet)

        public MarCell(){}
    }

}
