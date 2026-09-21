using VCare.SharedKernel.Results;

namespace Medications.Domain.Errors;

internal static class MedicationErrors
{
    // Also what a caller sees for an order in another care home: the query filter
    // hides it, and saying "not found" avoids confirming it exists elsewhere.
    public static readonly Error OrderNotFound =
        Error.NotFound("Medications.OrderNotFound", "Medication order not found.");
}
