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

    // Only ever returned after the password has checked out.
    public static readonly Error EmailNotConfirmed =
        Error.Forbidden("Auth.EmailNotConfirmed",
            "Your email address isn't verified yet. Use the verification link we sent to your email, or request a new link.");

    // Same message whether the email is unknown or the token is wrong, so this
    // endpoint can't be used to find out which emails are registered.
    public static readonly Error InvalidVerificationLink =
        Error.Validation("Auth.InvalidVerificationLink",
            "This verification link is invalid or has expired. Request a new link and try again.");
}
