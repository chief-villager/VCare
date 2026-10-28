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
        }

        public static async Task<IResult>GetCarePlan(
            ICarePlanService carePlanService, 
            Guid patientId, CancellationToken cancellationToken)
        {
            var response = await carePlanService.GetForPatientAsync(patientId, cancellationToken);
            return response.Value == null ? TypedResults.NotFound() : TypedResults.Ok(response);
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