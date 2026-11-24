using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Modules.CareHomes.Application.Contract.Dto
{
    /// <summary>
    /// TimeZoneId is an IANA zone such as "Europe/London". Dose times are times in
    /// the home, so it is required: guessing it puts every dose window off by the
    /// difference.
    /// </summary>
    public record  CreateCareHomeRequest( string Name, string Address, string Email, string TimeZoneId);
    public record UpdateCareHomeRequest( string? Name, string? Address, string? Email, string? TimeZoneId = null);
   
   
}