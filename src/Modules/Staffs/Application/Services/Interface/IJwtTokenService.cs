using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services.Interface
{
    public interface IJwtTokenService
    {
        Result<string> CreateToken(string name, StaffId staffId, Guid careHomeId,IEnumerable<string> roles);
    }
}