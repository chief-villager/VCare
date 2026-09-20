using src.Modules.CareHomes.Application.Contract.Dto;

namespace VCare.Api.Dtos;

// The admin's details, minus the role: the first staff member of a care home is
// always its admin, so letting the caller name a role would only let them pick
// the wrong one.
public sealed record CareHomeAdminInput(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Address,
    string UserName,
    string Password);

public sealed record RegisterCareHomeRequest(CreateCareHomeRequest CareHome, CareHomeAdminInput Admin);

public sealed record RegisterCareHomeResponse(Guid CareHomeId, Guid AdminStaffId);
