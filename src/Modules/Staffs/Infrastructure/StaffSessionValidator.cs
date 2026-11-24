using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Staffs.Infrastructure
{
    /// <summary>
    /// Decides on every request whether an access token still speaks for its holder.
    /// A token is signed and unexpired for its whole lifetime, so without this a
    /// demoted admin kept admin rights, and a deactivated carer kept access, until
    /// the token ran out. Identity changes the security stamp on a role change,
    /// password reset or deactivation; a token carrying an old stamp is refused.
    /// </summary>
    internal sealed class StaffSessionValidator(UserManager<ApplicationUser> userManager)
    {
        public const string StampClaim = "sstamp";

        public async Task<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
        {
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = principal.FindFirstValue(StampClaim);
            // A token from before stamps were issued has none, and is refused like a
            // stale one: its holder signs in again once.
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stamp))
                return false;

            var user = await userManager.FindByIdAsync(id);
            return user is not null
                && user.DeactivatedAt is null
                && string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal);
        }
    }
}
