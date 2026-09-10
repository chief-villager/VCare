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
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services
{
    internal class StaffService (IStaffRepository staffRepository, IAuthService authService, ICurrentUser currentUser) : IStaffService
    {
        
        public async Task<Result<StaffResponse>> GetStaffByIdAsync(StaffId id, CancellationToken cancellationToken)
        {
            var result = await staffRepository.GetByIdAsync(id, cancellationToken);
            if (result is null)
            {
                return Result.Failure<StaffResponse>("Staff not found");
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

        public async Task<Result<Guid>> CreateStaffAsync(CreateStaffRequest staffRequest, CancellationToken cancellationToken)
        {
            // New staff belong to the care home of the caller creating them.
            var careHomeId = currentUser.CareHomeId;
            var staff = Staff.Create(staffRequest.FirstName, staffRequest.LastName,
            staffRequest.Email, staffRequest.PhoneNumber, staffRequest.Role, staffRequest.Address, staffRequest.UserName, careHomeId);
            if (staff is null)
            {
                return Result.Failure<Guid>("Failed to create staff");
            }
            await staffRepository.AddAsync(staff, cancellationToken);
            var result = await authService.CreateApplicationUser( staff.Id.Value, staffRequest.UserName,
            staffRequest.Email, staffRequest.Password, staffRequest.PhoneNumber, staffRequest.Role, careHomeId);
            if (!result.IsSuccess)
            {
                return Result.Failure<Guid>("Unable to created application user");
            }
            await staffRepository.SaveChangesAsync(cancellationToken);
            return Result.Success(staff.Id.Value);
        }

        public async Task<Result> UpdateStaffAsync(StaffId staffId,UpdateStaffRequest staff, CancellationToken cancellationToken)
        {
            var existingStaff = await staffRepository.GetByIdAsync( staffId, cancellationToken);
            if (existingStaff is null)
            {
                return Result.Failure("Staff not found");
            }

            existingStaff.Update(staff.FirstName, staff.LastName, staff.Email, staff.PhoneNumber, staff.Role, staff.Address);
            staffRepository.Update(existingStaff);
            await staffRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result<string>> LoginStaffAsync( string userName, string password, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Result.Failure<string>("invalid login credentials");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return Result.Failure<string>("invalid login credentials");
            }
            var getStaff =  await staffRepository.GetStaffWithUserNameAsync(userName, token);
            if (getStaff == null)
            {
                return Result.Failure<string>("invalid login credentials");
            };
            var result = await authService.LoginAsync(userName, password, getStaff.Id, getStaff.CareHomeId.Value);
           if (result.IsFailure)
           {
             return Result.Failure<string>(result.Error);
           }
           return result;
           
        }

        public async Task<Result<string>> GenerateConfirmEmailLinkAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<string>("Email is required");
            }
            return await authService.GenerateConfirmEmailLink(email);
        }

        public async Task<Result<bool>> ConfirmEmailAsync(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return Result.Failure<bool>("Invalid confirmation link");
            }
            return await authService.ConfirmEmailAsync(email, token);
        }

        public async Task<Result<string>> RequestPasswordResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<string>("Email is required");
            }
            return await authService.GetPasswordResetcode(email);
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