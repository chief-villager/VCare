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

        /// <summary>Ends a session by revoking every live token in its family.</summary>
        Task<Result> LogoutAsync (string refreshToken, CancellationToken cancellationToken);

        Task<Result<string>> GenerateConfirmEmailLink( string email);

        Task<Result<bool>> ConfirmEmailAsync(string email, string code);

        Task<Result<string>> GetPasswordResetcode(string email);

        Task<Result<bool>> ResetPasswordAsync(string password, string email, string token);
    }
}
