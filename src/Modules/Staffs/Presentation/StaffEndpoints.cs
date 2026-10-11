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
using VCare.SharedKernel.Results;

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
            staff.MapPost("/{staffId:Guid}/document",UploadStaffDocument);
            staff.MapPost("/{staffId:Guid}/profile-image",UploadProfileImageForStaff);
            staff.MapGet("/{staffId:Guid}/document",GetStaffDocument);
        }

        public static async Task<IResult>UploadStaffDocument(Guid staffId, IFormFile formFile, string fileName, IStaffService staffService, CancellationToken cancellationToken)
        {
            var result = await staffService.UploadStaffDocumentAsync(new StaffId(staffId), formFile,fileName,cancellationToken);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { Key = result.Value });
        }

        public static async Task<IResult>UploadProfileImageForStaff(Guid staffId, IFormFile formFile, IStaffService staffService, CancellationToken cancellationToken)
        {
            var result = await staffService.UploadProfileImageForStaffAsync(new StaffId(staffId), formFile,cancellationToken);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { Key = result.Value });
        }
        public static async Task<IResult>GetStaffDocument(Guid staffId, string key, IStaffService staffService, CancellationToken cancellationToken)
        {
            var result = await staffService.GetStaffDocumentsAsync(new StaffId(staffId), key, cancellationToken);
            return result.IsFailure ? result.ToProblem() : TypedResults.File(result.Value.Content, result.Value.ContentType);
        }

        public static async Task<IResult>CreateStaff(CreateStaffRequest request, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.CreateStaffAsync(request,cancellationToken);
            return response.IsFailure 
                ? response.ToProblem() 
                : TypedResults.Created($"/api/staff/{response.Value}", response.Value);
        }

        public static async Task<IResult>GetStaff(Guid staffId, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.GetStaffByIdAsync(new StaffId(staffId),cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult>UpdateStaff(Guid staffId, UpdateStaffRequest request, 
            IStaffService staffService, CancellationToken cancellationToken)
        {
            var response = await staffService.UpdateStaffAsync(new StaffId(staffId),request,cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.NoContent();
        }
    }
}
