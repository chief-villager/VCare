using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Domain.Errors;

public static class PatientErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Patients.NotFound", $"Patient with id '{id}' was not found.");
}
