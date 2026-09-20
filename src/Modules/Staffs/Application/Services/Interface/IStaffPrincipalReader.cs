using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services.Interface
{
    /// <summary>What an access token needs to say about a staff member.</summary>
    public sealed record StaffPrincipal(string UserName, Guid CareHomeId, IReadOnlyList<string> Roles);

    /// <summary>
    /// Reads a staff member's current identity by id. Rotation needs it to reissue
    /// an access token without being told who the holder is, and reading it fresh
    /// means a role or care home changed since login takes effect on the next refresh.
    /// </summary>
    public interface IStaffPrincipalReader
    {
        Task<Result<StaffPrincipal>> GetByStaffIdAsync(StaffId staffId, CancellationToken cancellationToken);
    }
}
