using VCare.SharedKernel.Results;

namespace Medications.Domain.Errors;

internal static class MedicationErrors
{
    // Also what a caller sees for an order in another care home, or one reached
    // through another resident's URL: saying "not found" avoids confirming it
    // exists elsewhere.
    public static readonly Error OrderNotFound =
        Error.NotFound("Medications.OrderNotFound", "Medication order not found.");

    public static readonly Error PatientNotFound =
        Error.NotFound("Medications.PatientNotFound", "Resident not found.");

    public static readonly Error NotAScheduledSlot =
        Error.Validation("Medications.NotAScheduledSlot", "No dose is scheduled for this schedule at that time.");

    public static readonly Error DoseAlreadyRecorded =
        Error.Conflict("Medications.DoseAlreadyRecorded", "This dose is already recorded.");

    public static Error DoseNotYetDue(DateTime opensAt) =>
        Error.Conflict("Medications.DoseNotYetDue",
            $"This dose is not due yet. It can be signed from {opensAt:dd MMM HH:mm}.");

    public static readonly Error LateReasonRequired =
        Error.Validation("Medications.LateReasonRequired",
            "This dose is being recorded after its on-time window. Say why it is late.");

    public static readonly Error RecordingWindowClosed =
        Error.Conflict("Medications.RecordingWindowClosed",
            "It is too late to record this dose here. Tell the person in charge so it can be reviewed.");

    public static readonly Error MissedIsRecordedBySystem =
        Error.Validation("Medications.MissedIsRecordedBySystem",
            "Missed is recorded by the system. Record what happened instead, such as Refused, and why.");

    public static readonly Error AdministeredInFuture =
        Error.Validation("Medications.AdministeredInFuture", "A dose cannot be recorded as given at a time that has not happened yet.");

    public static readonly Error AdministeredOutsideWindow =
        Error.Validation("Medications.AdministeredOutsideWindow", "The time given is outside this dose's window.");

    public static readonly Error OutcomeNotFound =
        Error.Validation("Medications.OutcomeNotFound", "Unknown outcome.");

    public static readonly Error RecordedConcurrently =
        Error.Conflict("Medications.RecordedConcurrently",
            "Someone else recorded this dose at the same moment. Refresh and check before recording again.");

    public static readonly Error NotAPrnOrder =
        Error.Validation("Medications.NotAPrnOrder", "This is a regular order. Sign its scheduled doses instead.");

    public static readonly Error PrnUsesItsOwnEndpoint =
        Error.Validation("Medications.PrnUsesItsOwnEndpoint", "This is an as-required (PRN) order. Record it as a PRN dose.");

    public static readonly Error PrnDoseRequired =
        Error.Validation("Medications.PrnDoseRequired", "Record the dose given.");

    public static readonly Error PrnReasonRequired =
        Error.Validation("Medications.PrnReasonRequired", "Record why the as-required dose was given.");

    public static readonly Error PrnOutsideOrder =
        Error.Validation("Medications.PrnOutsideOrder", "The time given is outside the dates of this order.");

    public static Error PrnTooSoon(DateTime allowedFrom) =>
        Error.Conflict("Medications.PrnTooSoon",
            $"Too soon after the last dose. The next dose can be given from {allowedFrom:dd MMM HH:mm}.");

    public static Error PrnDailyLimitReached(int limit) =>
        Error.Conflict("Medications.PrnDailyLimitReached",
            $"The maximum of {limit} doses in 24 hours has been reached.");

    public static readonly Error ResidentLeftHome =
        Error.Conflict("Medications.ResidentLeftHome",
            "The resident had left the home by then. Nothing is recorded after a discharge or a death.");

    public static readonly Error OrderNotActiveThen =
        Error.Conflict("Medications.OrderNotActive", "The order was not active when this dose was due.");

    public static readonly Error StatusReasonRequired =
        Error.Validation("Medications.StatusReasonRequired", "Say why the order is being held, restarted or stopped.");

    public static readonly Error OrderDiscontinued =
        Error.Conflict("Medications.OrderDiscontinued", "This order has been discontinued. Record a new order instead.");

    public static Error StatusUnchanged(string status) =>
        Error.Conflict("Medications.StatusUnchanged", $"The order is already {status}.");

    public static Error OutcomeReasonRequired(string outcome) =>
        Error.Validation("Medications.OutcomeReasonRequired", $"Say why the dose was recorded as {outcome}.");

    public static readonly Error WitnessRequired =
        Error.Validation("Medications.WitnessRequired", "A controlled drug needs a second member of staff to witness it.");

    public static readonly Error WitnessMustBeSomeoneElse =
        Error.Validation("Medications.WitnessMustBeSomeoneElse", "The witness must be someone other than the person giving the dose.");

    public static readonly Error WitnessNotFound =
        Error.Validation("Medications.WitnessNotFound", "The witness is not a member of staff in this care home.");

    public static Error AllergyConflict(string substance) =>
        Error.Conflict("Medications.AllergyConflict",
            $"The resident has a recorded allergy to {substance}. Check with the prescriber; if they confirm the order, " +
            "send it again with allergyOverrideReason saying who confirmed it and why.");

    public static readonly Error InvalidMonth =
        Error.Validation("Medications.InvalidMonth", "Year and month must name a real month.");
}
