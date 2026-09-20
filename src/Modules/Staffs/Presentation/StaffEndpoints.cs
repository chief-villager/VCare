using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Staffs.Application.Dto;
using Staffs.Application.Services.Interface;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Presentation
{
    public static class StaffEndpoints
    {
        public static void AddStaffEndpoints(this IEndpointRouteBuilder app)
        {
            // New staff join the care home on the caller's token, so every route
            // here needs an authenticated caller to be scoped correctly.
            var staff = app.MapGroup("/api/staff").WithTags("Staff").RequireAuthorization();
            staff.MapPost("/",CreateStaff);
            staff.MapGet("/{staffId:Guid}",GetStaff);
            staff.MapPut("/{staffId:Guid}",UpdateStaff);
        }

        public static async Task<IResult>CreateStaff(CreateStaffRequest request, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.CreateStaffAsync(request,cancellationToken);
            return response.IsFailure 
                ? TypedResults.BadRequest(response.Error) 
                : TypedResults.Created($"/api/staff/{response.Value}", response.Value);
        }

        public static async Task<IResult>GetStaff(Guid staffId, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.GetStaffByIdAsync(new StaffId(staffId),cancellationToken);
            return response.IsFailure ? TypedResults.NotFound(response.Error) : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult>UpdateStaff(Guid staffId, UpdateStaffRequest request, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.UpdateStaffAsync(new StaffId(staffId),request,cancellationToken);
            return response.IsFailure ? TypedResults.NotFound(response.Error) : TypedResults.NoContent();
        }
    }
}
