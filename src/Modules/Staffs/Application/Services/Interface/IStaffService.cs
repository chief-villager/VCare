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
        Task<Result> UpdateStaffAsync(StaffId staffId, UpdateStaffRequest staff, CancellationToken cancellationToken);
        Task<Result<string>> LoginStaffAsync(string userName, string password, CancellationToken cancellationToken);

        /// <summary>Builds the callback link a staff member follows to confirm their email address.</summary>
        Task<Result<string>> GenerateConfirmEmailLinkAsync(string email);

        /// <summary>Confirms a staff member's email using the token issued by <see cref="GenerateConfirmEmailLinkAsync"/>.</summary>
        Task<Result<bool>> ConfirmEmailAsync(string email, string token);

        /// <summary>Builds the callback link a staff member follows to reset a forgotten password.</summary>
        Task<Result<string>> RequestPasswordResetAsync(string email);

        /// <summary>Sets a new password using the token issued by <see cref="RequestPasswordResetAsync"/>.</summary>
        Task<Result<bool>> ResetPasswordAsync(string email, string token, string password);
    }
}
