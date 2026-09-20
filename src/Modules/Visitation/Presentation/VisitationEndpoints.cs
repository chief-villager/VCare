using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VCare.Modules.Visitation.Application.Services;
using Visitation.Application.Services.Interfaces;

namespace Visitation.Presentation
{
    public static class VisitationEndpoints
    {
        public static void AddVisitationEndpoints(this IEndpointRouteBuilder app)
        {
            // Nested under the patient, like careplan and medication: a visit is
            // always a visit to someone. The visiting staff member and the care
            // home come off the caller's token, so the group needs an identity.
            var visit = app.MapGroup("/api/patient/{patientId:Guid}/visit").WithTags("Visitation").RequireAuthorization();
            visit.MapGet("/",GetVisitsForPatient);
            visit.MapPost("/check-in",CheckIn);
            visit.MapGet("/{visitId:Guid}",GetVisit);
            visit.MapPost("/{visitId:Guid}/check-out",CheckOut);
            visit.MapPost("/{visitId:Guid}/cancel",CancelVisit);
        }

        public static async Task<IResult>GetVisitsForPatient(Guid patientId, 
            IVisitationService visitationService, CancellationToken token)
        {
            var response = await visitationService.GetForPatientAsync(patientId,token);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult>CheckIn(Guid patientId, CheckInVisitRequest request, 
            IVisitationService visitationService, CancellationToken token)
        {
            var response = await visitationService.CheckInAsync(patientId,request,token);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.NoContent();
        }

        public static async Task<IResult>GetVisit(Guid visitId, 
            IVisitationService visitationService, CancellationToken token)
        {
            var response = await visitationService.GetByIdAsync(visitId,token);
            if (response.IsFailure)
                return TypedResults.BadRequest(response.Error);
            return response.Value is null ? TypedResults.NotFound() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult>CheckOut(Guid visitId, CheckOutVisitRequest request, 
            IVisitationService visitationService, CancellationToken token)
        {
            var response = await visitationService.CheckOutAsync(visitId,request,token);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.NoContent();
        }

        public static async Task<IResult>CancelVisit(Guid visitId, CancelVisitRequest request, 
            IVisitationService visitationService, CancellationToken token)
        {
            var response = await visitationService.CancelVisitAsync(visitId,request,token);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.NoContent();
        }
    }
}
