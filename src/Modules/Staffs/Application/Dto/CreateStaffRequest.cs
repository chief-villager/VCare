using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Staffs.Application.Dto
{
    public record CreateStaffRequest(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string Address,
        string Role
    );

    public record UpdateStaffRequest(
        string? FirstName= null,
        string? LastName = null,
        string? Email = null,
        string? PhoneNumber = null,
        string? Address = null,
        string? Role = null
    );

    public record StaffResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string Address,
        string Role
    );
    
}