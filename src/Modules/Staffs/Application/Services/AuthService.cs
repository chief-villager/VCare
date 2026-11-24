using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Staffs.Application.Dto;
using Staffs.Application.Services.Interface;
using Staffs.Domain;
using Staffs.Domain.Errors;
using Staffs.Infrastructure;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services
{
    internal class AuthService
    (
        UserManager<ApplicationUser> userManager, 
        RoleManager<ApplicationRole> roleManager,
        IRefreshToken refreshTokenService,
        IOptions<Hosting> options) : IAuthService
    {
        public async Task<Result> CreateApplicationUser(Guid staffId, string username, string email, string password, string phonenumber, string roleName, Guid careHomeId)
        {
            if (!IsAllowedRole(roleName))
            {
                return Result.Failure("Admin and care roles allowed only");
            }
            var doesRoleExist = await roleManager.RoleExistsAsync(roleName.ToUpper());
            if (!doesRoleExist)
            {
                return Result.Failure("Role does not exist");
            }
            var principal = new ApplicationUser
            {
                Id = staffId,
                Email = email,
                PhoneNumber = phonenumber,
                UserName = username,
                CareHomeId = careHomeId
            };
            
            // Identity reports a duplicate username, a duplicate email or a password
            // that fails policy through the result. Discarding it returned success
            // with no user created, and the caller went on believing it had one.
            var created = await userManager.CreateAsync(principal, password);
            if (!created.Succeeded)
            {
                return Result.Failure(Describe(created));
            }

            var assigned = await userManager.AddToRoleAsync(principal, roleName.ToUpper());
            if (!assigned.Succeeded)
            {
                // A user with no role authenticates but authorises nowhere. Better
                // no user at all than one nobody can explain later.
                await userManager.DeleteAsync(principal);
                return Result.Failure(Describe(assigned));
            }
            return Result.Success();
        }

        public async Task<Result> ValidateNewUserAsync(string username, string email, string password, string roleName)
        {
            if (!IsAllowedRole(roleName))
            {
                return Result.Failure("Admin and care roles allowed only");
            }
            if (!await roleManager.RoleExistsAsync(roleName.ToUpper()))
            {
                return Result.Failure("Role does not exist");
            }
            if (await userManager.FindByNameAsync(username) is not null)
            {
                return Result.Failure(Error.Conflict("Auth.UsernameTaken", "That username is already taken"));
            }
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                return Result.Failure(Error.Conflict("Auth.EmailRegistered", "That email address is already registered"));
            }

            // The configured password policy, run against an unsaved user, so the
            // caller hears "password too short" before anything has been written.
            var candidate = new ApplicationUser { UserName = username, Email = email };
            foreach (var validator in userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(userManager, candidate, password);
                if (!result.Succeeded)
                {
                    return Result.Failure(Describe(result));
                }
            }
            return Result.Success();
        }

        private static bool IsAllowedRole(string roleName) =>
            roleName.Equals(nameof(RoleEnum.Admin), StringComparison.CurrentCultureIgnoreCase)
            || roleName.Equals(nameof(RoleEnum.Carer), StringComparison.CurrentCultureIgnoreCase);

        private static string Describe(IdentityResult result) =>
            string.Join("; ", result.Errors.Select(e => e.Description));

        public async Task<Result<(string AccessToken, string RefreshToken)>> LoginAsync (string userName, string password, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByNameAsync(userName);
           
            if (user == null)
            {
                return Result.Failure<(string, string)>(Error.Unauthorized("Auth.InvalidCredentials", "invalid login credentials"));
            }

            // Accounts created before lockout was configured have it switched off;
            // turn it on the first time they are used.
            if (!user.LockoutEnabled)
            {
                await userManager.SetLockoutEnabledAsync(user, true);
            }

            // Checked before the password, and answered the same whatever the
            // password: otherwise a locked account would still say when a guess was right.
            if (await userManager.IsLockedOutAsync(user))
            {
                return Result.Failure<(string, string)>(LockedOut);
            }
            if (!await userManager.CheckPasswordAsync(user!, password))
            {
                await userManager.AccessFailedAsync(user);
                return Result.Failure<(string, string)>(await userManager.IsLockedOutAsync(user)
                    ? LockedOut
                    : Error.Unauthorized("Auth.InvalidCredentials", "invalid login credentials"));
            }
            await userManager.ResetAccessFailedCountAsync(user);

            // Only someone who already holds the password learns the account is deactivated.
            if (user.DeactivatedAt is not null)
            {
                return Result.Failure<(string, string)>(Error.Forbidden("Auth.Deactivated",
                    "This account has been deactivated. Speak to the care home's manager."));
            }

            // New staff must confirm their email before they can sign in. Checked
            // after the password, so only someone who already holds the password
            // learns that the account exists but is unconfirmed. CheckPasswordAsync
            // ignores Identity's SignIn.RequireConfirmedEmail option, so the check
            // has to be made here.
            if (!user.EmailConfirmed)
            {
                return Result.Failure<(string, string)>(Staffs.Domain.Errors.StaffErrors.EmailNotConfirmed);
            }
            var role = await userManager.GetRolesAsync(user);

            // Identity and care home are read off the stored user, never taken from
            // the caller: a caller-supplied care home would mint a token that walks
            // straight through the query filters every module isolates on.
            // Each login opens a new token family, so ending one session leaves the
            // same staff member's other sessions alone.
            return await refreshTokenService.CreateTokensAsync(
                userName, Guid.NewGuid(), new StaffId(user.Id), user.CareHomeId, role, user.SecurityStamp ?? string.Empty,
                cancellationToken);
        }

        public async Task<Result<(string AccessToken, string RefreshToken)>> RefreshAsync (string refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Result.Failure<(string, string)>("A refresh token is required.");
            }

            // Identity, care home and roles all come off the stored token's owner,
            // so nothing the caller sends beyond the token itself is trusted.
            return await refreshTokenService.RefreshTokenAsync(refreshToken, cancellationToken);
        }

        public async Task<Result> LogoutAsync (string refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Result.Failure("A refresh token is required.");
            }

            // The whole family goes, not just the token presented: every token
            // rotated out of this login dies with it, including one an attacker
            // may be holding from the middle of the chain.
            return await refreshTokenService.RevokeFamilyAsync(refreshToken, cancellationToken);
        }

        // The link points at the GET /api/auth/verify-email endpoint.
        public async Task<Result<string>> GenerateEmailVerificationLinkAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<string>(StaffErrors.InvalidVerificationLink);
            }
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = $"{options.Value.BaseUrl}/api/auth/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            return Result.Success(link);
        }

        public async Task<bool> NeedsEmailVerificationAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            return user is not null && !user.EmailConfirmed;
        }

        public async Task<Result> VerifyEmailAsync(string email, string token)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure(StaffErrors.InvalidVerificationLink);
            }
            // Following an old link after verifying is harmless; don't report it as an error.
            if (user.EmailConfirmed)
            {
                return Result.Success();
            }
            var result = await userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded ? Result.Success() : Result.Failure(StaffErrors.InvalidVerificationLink);
        }

        public async Task<Result<string?>> GetPasswordResetcode(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<string?>("Email is required");
            }

            // No account, an unconfirmed one, or a deactivated one: nothing to send,
            // and the same answer as for a real account, so the endpoint cannot be
            // used to find out who works where.
             var user = await userManager.FindByEmailAsync(email);
             if (user == null || !user.EmailConfirmed || user.DeactivatedAt is not null)
            {
                return Result.Success<string?>(null);
            }
            
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            // Points at the front-end page, not the API: following a link can't
            // supply the new password, so the page collects it and posts it on.
            var page = options.Value.ResetPasswordPageUrl;
            var separator = page.Contains('?') ? '&' : '?';
            var callbackurl = $"{page}{separator}email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            return Result.Success<string?>(callbackurl);
        }

         public async Task<Result<bool>> ResetPasswordAsync(string password, string email, string token)
        {
            // An unknown email reads like a bad link, so this cannot be used to
            // probe for accounts either.
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<bool>(InvalidResetLink);
            }
            var result = await userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
            {
                return Result.Failure<bool>(result.Errors.Any(e => e.Code == "InvalidToken")
                    ? InvalidResetLink
                    : Error.Validation("Auth.PasswordRejected", Describe(result)));
            }
            // Identity has changed the security stamp, so access tokens already stop
            // working; the refresh tokens go too, so a session someone else may hold
            // with the old password cannot be renewed.
            await refreshTokenService.RevokeAllForStaffAsync(new StaffId(user.Id), CancellationToken.None);
            await userManager.ResetAccessFailedCountAsync(user);
            return Result.Success(true);
        }

        private static readonly Error InvalidResetLink =
            Error.Validation("Auth.InvalidResetLink", "This password reset link is invalid or has expired. Request a new one.");

        private static readonly Error LockedOut = Error.Unauthorized("Auth.LockedOut",
            "Too many failed sign-in attempts. Try again in 15 minutes, or reset your password.");

        public async Task<Result> ChangeRoleAsync(Guid staffId, string role, CancellationToken cancellationToken)
        {
            if (!IsAllowedRole(role))
                return Result.Failure(Error.Validation("Staffs.InvalidRole", "The role must be Admin or Carer."));
            var user = await userManager.FindByIdAsync(staffId.ToString());
            if (user is null)
                return Result.Failure(StaffErrors.NotFound);

            var current = await userManager.GetRolesAsync(user);
            if (current.Count > 0)
            {
                var removed = await userManager.RemoveFromRolesAsync(user, current);
                if (!removed.Succeeded)
                    return Result.Failure(Describe(removed));
            }
            var added = await userManager.AddToRoleAsync(user, role.ToUpper());
            if (!added.Succeeded)
                return Result.Failure(Describe(added));

            // Tokens carry the role: the stamp change stops the ones already issued,
            // and the revoked refresh tokens cannot mint new ones under the old role.
            await userManager.UpdateSecurityStampAsync(user);
            await refreshTokenService.RevokeAllForStaffAsync(new StaffId(user.Id), cancellationToken);
            return Result.Success();
        }

        public async Task<Result> ChangeEmailAsync(Guid staffId, string email, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(staffId.ToString());
            if (user is null)
                return Result.Failure(StaffErrors.NotFound);
            if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                return Result.Success();
            if (await userManager.FindByEmailAsync(email) is not null)
                return Result.Failure(Error.Conflict("Auth.EmailRegistered", "That email address is already registered"));

            // SetEmailAsync clears EmailConfirmed and changes the security stamp: the
            // new address is confirmed before it can be used to sign in or reset.
            var changed = await userManager.SetEmailAsync(user, email);
            if (!changed.Succeeded)
                return Result.Failure(Describe(changed));
            await refreshTokenService.RevokeAllForStaffAsync(new StaffId(user.Id), cancellationToken);
            return Result.Success();
        }

        public async Task<Result> DeactivateAsync(Guid staffId, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(staffId.ToString());
            if (user is null)
                return Result.Failure(StaffErrors.NotFound);

            user.DeactivatedAt = DateTimeOffset.UtcNow;
            var updated = await userManager.UpdateAsync(user);
            if (!updated.Succeeded)
                return Result.Failure(Describe(updated));
            await userManager.UpdateSecurityStampAsync(user);
            await refreshTokenService.RevokeAllForStaffAsync(new StaffId(user.Id), cancellationToken);
            return Result.Success();
        }

        public async Task<Result> ReactivateAsync(Guid staffId, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(staffId.ToString());
            if (user is null)
                return Result.Failure(StaffErrors.NotFound);

            user.DeactivatedAt = null;
            var updated = await userManager.UpdateAsync(user);
            if (!updated.Succeeded)
                return Result.Failure(Describe(updated));
            await userManager.ResetAccessFailedCountAsync(user);
            return Result.Success();
        }

        public async Task<Result<ApplicationUserResponse>> GetApplicationUserByEmailAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<ApplicationUserResponse>(new Error("400", "Email is required", ErrorKind.Validation));
            }
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<ApplicationUserResponse>(new Error("404", "User not found", ErrorKind.NotFound));
            }
            return Result.Success(new ApplicationUserResponse(user.Id, user.UserName!, user.Email!, user.CareHomeId));
        }
    }
}