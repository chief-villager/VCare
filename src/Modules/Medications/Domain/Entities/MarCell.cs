namespace Medications.Domain.Entities
{
    internal class MarCell
    {
        // Null on a PRN entry: as-required doses are timestamped, not slotted.
        public DateTime? DueAt { get; set; }
        // Carried from the slot: a phased schedule can change the dose part-way
        // through the month, so it belongs to the square, not the row. On a PRN
        // entry it is the dose actually given.
        public string? Dose { get; set; }
        public MedicationAdministration? Administration { get; set; }    // null = nothing recorded
        public OutcomeCode? Outcome { get; set; }
        // When the chart was built. Every cell of one chart judges against the same instant.
        public DateTime Now { get; set; }
        // When the order was entered into VCare.
        public DateTime OrderEnteredAt { get; set; }
        // The resident was in hospital or on leave when the dose was due.
        public bool IsDuringAbsence { get; set; }

        // Nothing recorded yet, and the resident was away: not the home's dose to give.
        public bool IsAway => Administration is null && DueAt is not null && IsDuringAbsence;

        // Nothing recorded, and the window had closed before the order existed in
        // VCare: not a missed dose, just a square from before the system's time.
        public bool IsBeforeOrderEntered => Administration is null && DueAt is { } due
            && DoseWindow.ClosedBeforeOrderEntered(due, OrderEnteredAt);

        // Missed is either stored (the recorder wrote a "Missed" row) or not yet
        // stored (the window closed and the recorder has not run since). Both read
        // the same: DoseWindow decides, nothing else.
        public bool IsMissed => Administration is null
            ? DueAt is { } due && !IsBeforeOrderEntered && !IsAway && DoseWindow.HasClosed(due, Now)
            : Administration.IsMissed;

        // A carer recorded an outcome: Taken, Refused...
        public bool IsSigned => Administration is not null && !Administration.IsMissed;

        // Inside the window around DueAt: still signable on time.
        public bool IsDue => Administration is null && !IsAway && DueAt is { } due && DoseWindow.IsDue(due, Now);
        // (none of these = not due yet)

        public MarCell(){}
    }

}
