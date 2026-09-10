using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Staffs.Application.Services.Interface;
using Staffs.Domain;
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
        IJwtTokenService jwtTokenService,
        IOptions<Hosting> options) : IAuthService
    {
        public async Task<Result> CreateApplicationUser(Guid staffId, string username, string email, string password, string phonenumber, string roleName, Guid careHomeId)
        {
            if (!roleName.Equals(nameof(RoleEnum.Admin), StringComparison.CurrentCultureIgnoreCase) 
            && !roleName.Equals(nameof(RoleEnum.Carer), StringComparison.CurrentCultureIgnoreCase))
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
                Email = username,
                PhoneNumber = phonenumber,
                UserName = username,
                CareHomeId = careHomeId
            };
            
            await userManager.CreateAsync(principal, password);
            await userManager.AddToRoleAsync(principal, roleName);
            return Result.Success();
        }

        public async Task<Result<string>> LoginAsync (string userName, string password, StaffId staffId, Guid careHomeId)
        {
            var user = await userManager.FindByNameAsync(userName);
            // userManager.GenerateEmailConfirmationTokenAsync()
           
            if (user == null)
            {
                return Result.Failure<string>("invalid login credentials");
            }
            if (!await userManager.CheckPasswordAsync(user!, password))
            {
               return Result.Failure<string>("invalid login credentials");
            }
            var role = await userManager.GetRolesAsync(user);
            return jwtTokenService.CreateToken(userName,staffId, careHomeId, role);
            
        }

        public async Task<Result<string>>GenerateConfirmEmailLink(string email)
        {   
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<string>("Invalid user");
            }
            string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackurl = $"{options.Value.BaseUrl}/api/auth/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(code)}";
            return Result.Success<string>(callbackurl);
        }

        public async Task<Result<bool>> ConfirmEmailAsync(string email, string code)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<bool>("Invalid user");
            }
            var result = await userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                return Result.Failure<bool>(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            return Result.Success(true);
        }

        public async Task<Result<string>> GetPasswordResetcode(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<string>("Email is required");
            }

             var user = await userManager.FindByEmailAsync(email);
             if (user == null)
            {
                return Result.Failure<string>("Invalid user");
            }
             if (!user.EmailConfirmed)
             {
                return Result.Failure<string>("Valid email required");
             }
            
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var callbackurl = $"{options.Value.BaseUrl}/api/auth/Reset_password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            return Result.Success<string>(callbackurl);
        }

         public async Task<Result<bool>> ResetPasswordAsync(string password, string email, string token)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<bool>("Invalid user");
            }
            var result = await userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
            {
                return Result.Failure<bool>(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            return Result.Success(true);
        } 

       
    }
}