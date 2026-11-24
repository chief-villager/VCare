using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Staffs.Application.Dto;
using Staffs.Infrastructure;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services.Interface
{
    public interface IAuthService
    {
        
        Task<Result> CreateApplicationUser (Guid staffId, string username, string email, 
        string password, string phonenumber, string roleName, Guid careHomeId);
        Task<Result<ApplicationUserResponse>> GetApplicationUserByEmailAsync(string email, CancellationToken cancellationToken);
        Task<Result<(string AccessToken, string RefreshToken)>> LoginAsync (string userName, string password, CancellationToken cancellationToken);

        /// <summary>Exchanges a refresh token for a new pair, rotating the old one out.</summary>
        Task<Result<(string AccessToken, string RefreshToken)>> RefreshAsync (string refreshToken, CancellationToken cancellationToken);

        /// <summary>
        /// Checks that a user could be created with these details, writing nothing.
        /// Lets a caller spanning two modules fail before the first one commits.
        /// </summary>
        Task<Result> ValidateNewUserAsync (string username, string email, string password, string roleName);

        /// <summary>Ends a session by revoking every live token in its family.</summary>
        Task<Result> LogoutAsync (string refreshToken, CancellationToken cancellationToken);

        /// <summary>Builds the link a user follows to verify their email address.</summary>
        Task<Result<string>> GenerateEmailVerificationLinkAsync(string email);

        /// <summary>True only when a user with this email exists and has not verified it.</summary>
        Task<bool> NeedsEmailVerificationAsync(string email);

        /// <summary>Verifies the user's email with the token from a <see cref="GenerateEmailVerificationLinkAsync"/> link.</summary>
        Task<Result> VerifyEmailAsync(string email, string token);

        /// <summary>The reset link to email, or null when there is nobody to send it to.
        /// Never a failure for an unknown email, so the caller cannot tell the difference.</summary>
        Task<Result<string?>> GetPasswordResetcode(string email);

        /// <summary>Moves the sign-in role and ends every session, so the old role stops working now.</summary>
        Task<Result> ChangeRoleAsync(Guid staffId, string role, CancellationToken cancellationToken);

        /// <summary>Moves the sign-in email. It must be confirmed again before the next sign-in.</summary>
        Task<Result> ChangeEmailAsync(Guid staffId, string email, CancellationToken cancellationToken);

        /// <summary>Stops the staff member signing in and ends every session they have.</summary>
        Task<Result> DeactivateAsync(Guid staffId, CancellationToken cancellationToken);

        Task<Result> ReactivateAsync(Guid staffId, CancellationToken cancellationToken);

        Task<Result<bool>> ResetPasswordAsync(string password, string email, string token);
    }
}
