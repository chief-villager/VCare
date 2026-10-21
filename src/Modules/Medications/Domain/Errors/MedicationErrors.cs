using VCare.SharedKernel.Results;

namespace Medications.Domain.Errors;

internal static class MedicationErrors
{
    // Also what a caller sees for an order in another care home: the query filter
    // hides it, and saying "not found" avoids confirming it exists elsewhere.
    public static readonly Error OrderNotFound =
        Error.NotFound("Medications.OrderNotFound", "Medication order not found.");

    public static readonly Error NotAScheduledSlot =
        Error.Validation("Medications.NotAScheduledSlot", "No dose is scheduled for this schedule at that time.");

    public static readonly Error DoseAlreadyRecorded =
        Error.Conflict("Medications.DoseAlreadyRecorded", "This dose is already recorded.");

    public static readonly Error AdministrationWindowElapsed =
        Error.Conflict("Medications.AdministrationWindowElapsed", "The window for this dose has passed; it has been recorded as missed.");
}
