using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Staffs.Application.Dto;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services.Interface
{
    public interface IStaffService 
    {
        Task<Result<StaffResponse>> GetStaffByIdAsync(StaffId id, CancellationToken cancellationToken);
        Task<Result<Guid>> CreateStaffAsync(CreateStaffRequest staffRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Creates staff in a care home named explicitly rather than taken from the
        /// caller's token. Registering a care home needs this: the caller creating
        /// the first admin has no care home of their own yet.
        /// </summary>
        Task<Result<Guid>> CreateStaffForCareHomeAsync(Guid careHomeId, CreateStaffRequest staffRequest, CancellationToken cancellationToken);

        /// <summary>Checks these staff details could be created, writing nothing.</summary>
        Task<Result> ValidateNewStaffAsync(CreateStaffRequest staffRequest);
        Task<Result> UpdateStaffAsync(StaffId staffId, UpdateStaffRequest staff, CancellationToken cancellationToken);
        Task<Result<(string AccessToken, string RefreshToken)>> LoginStaffAsync(string userName, string password, CancellationToken cancellationToken);

        /// <summary>Exchanges a refresh token for a new pair, rotating the old one out.</summary>
        Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

        /// <summary>Ends a session by revoking every live token in its family.</summary>
        Task<Result> LogoutStaffAsync(string refreshToken, CancellationToken cancellationToken);

        /// <summary>Builds the callback link a staff member follows to confirm their email address.</summary>
        Task<Result<string>> GenerateConfirmEmailLinkAsync(string email);

        /// <summary>Confirms a staff member's email using the token issued by <see cref="GenerateConfirmEmailLinkAsync"/>.</summary>
        Task<Result<bool>> ConfirmEmailAsync(string email, string token);

        /// <summary>Builds the callback link a staff member follows to reset a forgotten password.</summary>
        Task<Result> RequestPasswordResetAsync(string email);

        /// <summary>Sets a new password using the token issued by <see cref="RequestPasswordResetAsync"/>.</summary>
        Task<Result<bool>> ResetPasswordAsync(string email, string token, string password);
    }
}
