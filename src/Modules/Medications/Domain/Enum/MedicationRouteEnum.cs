using VCare.SharedKernel.Results;

namespace Medications.Domain.Enum
{

    public enum MedicationRouteEnum
    {
        ORAL,
        SUBLINGUAL,
        BUCCAL,
        NASAL,
        OPHTHALMIC,
        OTIC,
        TOPICAL,
        TRANSDERMAL,
        INHALATION,
        RECTAL,
        VAGINAL,
        INTRAVENOUS,
        INTRAMUSCULAR,
        SUBCUTANEOUS,
        INTRADERMAL,
        INTRAOSSEOUS,
        EPIDURAL,
        INTRATHECAL,
        ENTERAL,
        GASTROSTOMY,
        JEJUNOSTOMY,
        NEBULISATION
    }

    public static class RouteEnumChecker
    {
        // Built from the enum itself, so a new route is accepted as soon as it's added.
        // Exact names only: unlike Enum.TryParse, this rejects numbers ("5") and
        // comma-joined names ("ORAL, NASAL"). System.Enum is spelled out because this
        // namespace is itself called Enum.
        private static readonly Dictionary<string, MedicationRouteEnum> Routes =
            System.Enum.GetValues<MedicationRouteEnum>()
                .ToDictionary(r => r.ToString(), StringComparer.OrdinalIgnoreCase);

        public static Result<MedicationRouteEnum> MedicationRouteChecker(string route)
        {
            if (!string.IsNullOrWhiteSpace(route) && Routes.TryGetValue(route.Trim(), out var parsed))
                return Result.Success(parsed);

            return Result.Failure<MedicationRouteEnum>(
                new Error("400",
                    $"Invalid medication route '{route}'. Expected one of: {string.Join(", ", Routes.Keys)}.",
                    ErrorKind.Validation));
        }
    }

}
