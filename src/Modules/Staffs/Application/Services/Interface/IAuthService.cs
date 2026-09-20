using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services.Interface
{
    public interface IAuthService
    {
        
        Task<Result> CreateApplicationUser (Guid staffId, string username, string email, 
        string password, string phonenumber, string roleName, Guid careHomeId);

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

        Task<Result<string>> GenerateConfirmEmailLink( string email);

        Task<Result<bool>> ConfirmEmailAsync(string email, string code);

        Task<Result<string>> GetPasswordResetcode(string email);

        Task<Result<bool>> ResetPasswordAsync(string password, string email, string token);
    }
}
