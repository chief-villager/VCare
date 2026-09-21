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
            var careplan = app.MapGroup("/api/patient/{patientId:Guid}/careplan").WithTags("CarePlan");
            careplan.MapGet("/", GetCarePlan);
            careplan.MapPost("/",CreateCarePlan);
            careplan.MapPut("/{carePlanId:Guid}",UpdateCarePlan);
        }

        public static async Task<IResult>GetCarePlan(
            ICarePlanService carePlanService, 
            Guid patientId, CancellationToken cancellationToken)
        {
            var response = await carePlanService.GetForPatientAsync(patientId, cancellationToken);
            return response.Value == null ? TypedResults.NotFound() : TypedResults.Ok(response);
        }

        public static async Task<IResult> CreateCarePlan(
            ICarePlanService carePlanService, CreateCarePlanRequest request, 
            CancellationToken cancellationToken)
        {
           var response =  await carePlanService.CreateCarePlanAsync(request, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Created(response.Value.ToString());
        }


        public static async Task<IResult> UpdateCarePlan( Guid carePlanId,
            ICarePlanService carePlanService, UpdateCarePlanRequest request, 
            CancellationToken cancellationToken, ICurrentUser currentUser)
        {
            await carePlanService.GetForPatientAsync(carePlanId, cancellationToken);
            var response =  await carePlanService.UpdateCarePlanAsync(carePlanId,request, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok();
        }


    }
}