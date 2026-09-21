using VCare.SharedKernel.Results;

namespace Staffs.Domain.Errors;

internal static class StaffErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Staffs.NotFound", "Staff not found.");

    // One message for every way a login can fail. Naming which half was wrong
    // tells a probing client whether the username exists.
    public static readonly Error InvalidLogin =
        Error.Unauthorized("Auth.InvalidCredentials", "invalid login credentials");
}
