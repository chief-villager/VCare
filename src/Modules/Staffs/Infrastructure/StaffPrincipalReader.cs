using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Staffs.Application.Services.Interface;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Infrastructure
{
    // Deliberately not part of AuthService: RefreshTokenGenerator depends on this
    // and AuthService depends on RefreshTokenGenerator, so folding the two together
    // would close a dependency cycle the container cannot resolve.
    internal sealed class StaffPrincipalReader(UserManager<ApplicationUser> userManager) : IStaffPrincipalReader
    {
        public async Task<Result<StaffPrincipal>> GetByStaffIdAsync(StaffId staffId, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(staffId.Value.ToString());
            if (user is null)
                return Result.Failure<StaffPrincipal>(Error.Unauthorized("Auth.InvalidToken", "Invalid Token"));

            var roles = await userManager.GetRolesAsync(user);
            return Result.Success(new StaffPrincipal(user.UserName!, user.CareHomeId, [.. roles]));
        }
    }
}
