using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CarePlans.Application.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VCare.Modules.CarePlans.Application.Services;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace CarePlans.Presentation
{
    public static class CarePlanEndpoints
    {
        public static void AddCarePlanEndpoint(this IEndpointRouteBuilder app)
        {
            var careplan = app.MapGroup("/api/patient/{patientId:Guid}/careplan").WithTags("CarePlan").RequireAuthorization();
            careplan.MapGet("/", GetCarePlan).RequireAuthorization("Admin_Or_Carer");
            careplan.MapPost("/",CreateCarePlan).RequireAuthorization("Admin_Role");
            careplan.MapPut("/{carePlanId:Guid}",UpdateCarePlan).RequireAuthorization("Admin_Role");

            // Every version the plan has had, and the plan as it stood at any one.
            careplan.MapGet("/versions", GetVersions).RequireAuthorization("Admin_Or_Carer");
            careplan.MapGet("/versions/{number:int}", GetVersion).RequireAuthorization("Admin_Or_Carer");
        }

        public static async Task<IResult> GetVersions(Guid patientId, ICarePlanService carePlanService, CancellationToken cancellationToken)
        {
            var response = await carePlanService.GetVersionsAsync(patientId, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult> GetVersion(Guid patientId, int number, ICarePlanService carePlanService, CancellationToken cancellationToken)
        {
            var response = await carePlanService.GetVersionAsync(patientId, number, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult>GetCarePlan(
            ICarePlanService carePlanService, 
            Guid patientId, CancellationToken cancellationToken)
        {
            var response = await carePlanService.GetForPatientAsync(patientId, cancellationToken);
            if (response.IsFailure)
                return response.ToProblem();
            // The plan itself, not the Result wrapper around it (which serialised as
            // { value, isSuccess, error } and made every client unwrap it).
            return response.Value is null ? TypedResults.NotFound() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult> CreateCarePlan(
            Guid patientId, ICarePlanService carePlanService, CreateCarePlanRequest request, 
            CancellationToken cancellationToken)
        {
           var response =  await carePlanService.CreateCarePlanAsync(patientId, request, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Created(response.Value.ToString());
        }


        public static async Task<IResult> UpdateCarePlan(Guid patientId, Guid carePlanId,
            ICarePlanService carePlanService, UpdateCarePlanRequest request, 
            CancellationToken cancellationToken)
        {
            var response =  await carePlanService.UpdateCarePlanAsync(patientId, carePlanId, request, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok();
        }


    }
}