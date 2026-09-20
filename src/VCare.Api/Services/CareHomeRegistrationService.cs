using src.Modules.CareHomes.Application.Contract;
using Staffs.Application.Dto;
using Staffs.Application.Services.Interface;
using Staffs.Domain;
using VCare.Api.Dtos;
using VCare.SharedKernel.Results;

namespace VCare.Api.Services
{
    // A care home and its first admin are created together: neither module knows
    // about the other, so the policy that joins them lives at the composition
    // root, the same way the dashboard composes its two reads.
    //
    // It has to be one call. Staff creation takes the care home from the caller's
    // token, and the only way to get a token is to be staff already, so a care
    // home created on its own is a tenant nobody can ever sign into.
    public class CareHomeRegistrationService(ICareHomeService careHomeService, IStaffService staffService)
    {
        public async Task<Result<RegisterCareHomeResponse>> RegisterAsync(
            RegisterCareHomeRequest request, CancellationToken cancellationToken)
        {
            var admin = new CreateStaffRequest(
                FirstName: request.Admin.FirstName,
                LastName: request.Admin.LastName,
                Email: request.Admin.Email,
                PhoneNumber: request.Admin.PhoneNumber,
                Address: request.Admin.Address,
                Role: nameof(RoleEnum.Admin),
                Password: request.Admin.Password,
                UserName: request.Admin.UserName);

            // Checked before anything is written. A taken username or a password
            // that fails policy is the likely failure here, and finding out after
            // the care home has committed means compensating for no reason.
            var canCreateAdmin = await staffService.ValidateNewStaffAsync(admin);
            if (canCreateAdmin.IsFailure)
                return Result.Failure<RegisterCareHomeResponse>(canCreateAdmin.Error);

            var careHome = await careHomeService.RegisterCareHomeAsync(request.CareHome, cancellationToken);
            if (careHome.IsFailure)
                return Result.Failure<RegisterCareHomeResponse>(careHome.Error);

            if (!Guid.TryParse(careHome.Value, out var careHomeId))
                return Result.Failure<RegisterCareHomeResponse>("Care home was created with an unreadable id.");

            var adminStaff = await staffService.CreateStaffForCareHomeAsync(careHomeId, admin, cancellationToken);
            if (adminStaff.IsFailure)
            {
                // Two DbContexts, two SaveChanges, no shared transaction to roll
                // back: undo the care home by hand rather than leave one stranded.
                await careHomeService.DeleteCareHomeAsync(careHomeId, cancellationToken);
                return Result.Failure<RegisterCareHomeResponse>(adminStaff.Error);
            }

            return Result.Success(new RegisterCareHomeResponse(careHomeId, adminStaff.Value));
        }
    }
}
