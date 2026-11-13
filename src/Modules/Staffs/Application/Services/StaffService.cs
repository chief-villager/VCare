using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;
using Staffs.Application.Abstraction;
using Staffs.Application.Dto;
using Staffs.Application.Services.Interface;
using Staffs.Domain.Entity;
using Staffs.Infrastructure;
using VCare.SharedKernel.Abstractions;
using Staffs.Domain.Errors;
using VCare.SharedKernel.Results;
using Staffs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VCare.SharedKernel.Domain;

namespace Staffs.Application.Services
{
    internal class StaffService (
        IStaffRepository staffRepository, 
        IAuthService authService, 
        ICurrentUser currentUser,
        IDocumentService documentService, IOptions<StorageEntity> options,
        INotification notification,
        ILogger<StaffService> logger) : IStaffService
    {
        //move this to congiguration file later.

        public  async Task<Result<string>> UploadStaffDocumentAsync(StaffId staffId, IFormFile formFile,string fileName, CancellationToken cancellationToken)
        {
            if(fileName == null || fileName.Trim().Length == 0)
            {
                return Result.Failure<string>(new Error("400", "File name is required", ErrorKind.Validation));
            }
            var staff = await staffRepository.GetByIdAsync(staffId, cancellationToken);
            if (staff is null)
            {
                return Result.Failure<string>(new Error("404", "Staff not found", ErrorKind.NotFound));
            }
            if(currentUser.CareHomeId != staff.CareHomeId.Value)
            {
                return Result.Failure<string>(new Error("403", "You are not authorized to upload documents for this staff", ErrorKind.Forbidden));
            }
            if (formFile.Length <= 0)
            {
                return Result.Failure<string>(new Error("400", "The uploaded file is empty.", ErrorKind.Validation));
            }
            await using var content = formFile.OpenReadStream();
            var key = await documentService.WritingAnObjectAsync(options.Value.BucketName, content, formFile.FileName,
                formFile.ContentType, currentUser.CareHomeId.ToString(), cancellationToken);
            if (key.IsFailure)
            {
                return Result.Failure<string>(key.Error);
            }
            var documentResult = staff.AddDocument(fileName, key.Value);
            if (documentResult.IsFailure)
            {
                return Result.Failure<string>(documentResult.Error);
            }
            await staffRepository.SaveChangesAsync(cancellationToken);
            return Result.Success(key.Value);
        }

            
        public async Task<Result<string>> UploadProfileImageForStaffAsync(StaffId staffId, IFormFile formFile, CancellationToken cancellationToken)
        {
            if (formFile.Length <= 0)
            {
                return Result.Failure<string>(new Error("400", "The uploaded file is empty.", ErrorKind.Validation));
            }
            var staff = await staffRepository.GetByIdAsync(staffId, cancellationToken);
            if (staff is null)
            {
                return Result.Failure<string>(new Error("404", "Staff not found", ErrorKind.NotFound));
            }
            if (currentUser.CareHomeId != staff.CareHomeId.Value)
            {
                return Result.Failure<string>(new Error("403", "You are not authorized to upload profile image for this staff", ErrorKind.Forbidden));
            }
            await using var content = formFile.OpenReadStream();
            var key = await documentService.UploadProfileImageAsync(options.Value.BucketName, content, formFile.FileName,
                formFile.ContentType, currentUser.CareHomeId.ToString(), cancellationToken);
            if (key.IsFailure)
            {
                return key;
            }
            var updated = staff.UpdateProfilePicture(key.Value);
            if (updated.IsFailure)
            {
                return Result.Failure<string>(updated.Error);
            }
            await staffRepository.SaveChangesAsync(cancellationToken);
            return key;
        }

        public async Task<Result<(Stream Content, string ContentType)>> GetStaffDocumentsAsync(StaffId staffId, string key, CancellationToken cancellationToken)
        {
            var staff = await staffRepository.GetByIdAsync(staffId, cancellationToken);

            if (staff is null)
            {
                return Result.Failure<(Stream Content, string ContentType)>(new Error("404", "Staff not found", ErrorKind.NotFound));
            }

            if (currentUser.CareHomeId != staff.CareHomeId.Value)
            {
                return Result.Failure<(Stream Content, string ContentType)>(new Error("403", "You are not authorized to view documents for this staff", ErrorKind.Forbidden));
            }
            var document = await documentService.GetObjectAsync(objectKey: key, sourceBucket: options.Value.BucketName, cancellationToken);
            return Result.Success(document);
        }
        
        public async Task<Result<StaffResponse>> GetStaffByIdAsync(StaffId id, CancellationToken cancellationToken)
        {
            var result = await staffRepository.GetByIdAsync(id, cancellationToken);
            if (result is null)
            {
                return Result.Failure<StaffResponse>(StaffErrors.NotFound);
            }
            return Result.Success(new StaffResponse(
                Id: result.Id.Value,
                FirstName: result.FirstName,
                LastName: result.LastName,
                Email: result.Email,
                PhoneNumber: result.PhoneNumber,
                Address: result.Address,
                Role: result.Role
            ));
        }

        // New staff belong to the care home of the caller creating them.
        public Task<Result<CreateStaffResponse>> CreateStaffAsync(CreateStaffRequest staffRequest, CancellationToken cancellationToken) =>
            CreateStaffForCareHomeAsync(currentUser.CareHomeId, staffRequest, cancellationToken);

        public Task<Result> ValidateNewStaffAsync(CreateStaffRequest staffRequest) =>
            authService.ValidateNewUserAsync(staffRequest.UserName, staffRequest.Email,
                staffRequest.Password, staffRequest.Role);

        public async Task<Result<CreateStaffResponse>> CreateStaffForCareHomeAsync(Guid careHomeId, CreateStaffRequest staffRequest, CancellationToken cancellationToken)
        {
            if (careHomeId == Guid.Empty)
            {
                return Result.Failure<CreateStaffResponse>(Error.Forbidden("Staffs.NoCareHome", "A care home is required to create staff"));
            }
            var staff = Staff.Create(staffRequest.FirstName, staffRequest.LastName,
            staffRequest.Email, staffRequest.PhoneNumber, staffRequest.Role, staffRequest.Address, staffRequest.UserName, careHomeId);
            if (staff is null)
            {
                return Result.Failure<CreateStaffResponse>("Failed to create staff");
            }
            
            // The Identity user goes first and commits on its own. Tracking the Staff
            // only afterwards keeps its insert inside the transaction
            // CommitandSaveAsync opens; Identity's save would otherwise commit it early.
            var result = await authService.CreateApplicationUser( staff.Id.Value, staffRequest.UserName,
            staffRequest.Email, staffRequest.Password, staffRequest.PhoneNumber, staffRequest.Role, careHomeId);
            if (result.IsFailure)
            {
                return Result.Failure<CreateStaffResponse>(result.Error);
            }
            await staffRepository.AddAsync(staff, cancellationToken);
            await staffRepository.CommitandSaveAsync(cancellationToken);

            // Sent only once the staff member is saved, and outside the transaction,
            // so a slow or failing mail server can't hold or roll back the insert.
            await SendEmailVerificationLinkAsync(staffRequest.Email, staffRequest.UserName, cancellationToken);
            return Result.Success(new CreateStaffResponse(
                staff.Id.Value,
                staff.Email,
                staff.Role
            ));
        }

        public async Task<Result> UpdateStaffAsync(StaffId staffId,UpdateStaffRequest staff, CancellationToken cancellationToken)
        {
            var existingStaff = await staffRepository.GetByIdAsync( staffId, cancellationToken);
            if (existingStaff is null)
            {
                return Result.Failure(StaffErrors.NotFound);
            }

            existingStaff.Update(staff.FirstName, staff.LastName, staff.Email, staff.PhoneNumber, staff.Role, staff.Address);
            staffRepository.Update(existingStaff);
            await staffRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result<(string AccessToken, string RefreshToken)>> LoginStaffAsync( string userName, string password, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Result.Failure<(string, string)>(StaffErrors.InvalidLogin);
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return Result.Failure<(string, string)>(StaffErrors.InvalidLogin);
            }

            // No staff lookup first. Staffs sits behind a care-home query filter and
            // the caller is unauthenticated here, so that filter matches nothing and
            // turned every login into "invalid credentials". AuthService reads the
            // identity and care home off the stored user instead.
            // An unverified email is refused with EmailNotConfirmed, which tells the
            // staff member to use the link sent when their account was created or to
            // request a new one. Nothing is sent from here.
            return await authService.LoginAsync(userName, password, token);
        }

        public async Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string refreshToken, CancellationToken token)
        {
            return await authService.RefreshAsync(refreshToken, token);
        }

        public async Task<Result> LogoutStaffAsync(string refreshToken, CancellationToken token)
        {
            return await authService.LogoutAsync(refreshToken, token);
        }

        // Sends the same email as account creation. Always succeeds for a
        // well-formed email, whether or not an account exists or is already
        // verified, so the endpoint reveals nothing about which emails are
        // registered. The link itself only ever goes by email.
        public async Task<Result> ResendEmailVerificationLinkAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure(Error.Validation("Auth.EmailRequired", "Email is required"));
            }
            if (!await authService.NeedsEmailVerificationAsync(email))
            {
                return Result.Success();
            }
            var user = await authService.GetApplicationUserByEmailAsync(email, cancellationToken);
            if (user.IsSuccess)
            {
                await SendEmailVerificationLinkAsync(user.Value.Email, user.Value.UserName, cancellationToken);
            }
            return Result.Success();
        }

        // Sent straight to the notification service, not queued. A failure is
        // logged rather than returned: the account already exists, and the staff
        // member can ask for the link again through the resend endpoint.
        private async Task SendEmailVerificationLinkAsync(string email, string userName, CancellationToken cancellationToken)
        {
            var link = await authService.GenerateEmailVerificationLinkAsync(email);
            if (link.IsFailure)
            {
                logger.LogWarning("Could not build an email verification link for {Email}: {Error}", email, link.Error.Description);
                return;
            }
            try
            {
                var sent = await notification.SendEmailVerificationLinkAsync(email, userName, link.Value, cancellationToken);
                if (sent.IsFailure)
                {
                    logger.LogWarning("Email verification link to {Email} was not sent: {Error}", email, sent.Error.Description);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sending the email verification link to {Email} failed.", email);
            }
        }

        public async Task<Result> VerifyEmailAsync(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure(StaffErrors.InvalidVerificationLink);
            }
            return await authService.VerifyEmailAsync(email, token);
        }

        public async Task<Result> RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure("Email is required");
            }
            var callbackUrl =  await authService.GetPasswordResetcode(email);
            if (callbackUrl.IsFailure)
            {
                return Result.Failure(callbackUrl.Error);
            }

            // Sent straight to the notification service, not queued. Unlike the
            // verification link, a failure is returned: nothing else was changed, so
            // the caller can simply ask again.
            try
            {
                return await notification.SendPasswordResetLinkAsync(email, callbackUrl.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sending the password reset link to {Email} failed.", email);
                return Result.Failure(Error.Validation("Auth.ResetEmailFailed",
                    "We couldn't send the password reset email. Please try again."));
            }
        }

        public async Task<Result<bool>> ResetPasswordAsync(string email, string token, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure<bool>("Invalid password reset link");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return Result.Failure<bool>("Password is required");
            }
            return await authService.ResetPasswordAsync(password, email, token);
        }
    }
   
}