using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Application.Services;
using Medications.Application.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Medications.Presentation
{
    public static class MedicationEndpoints
    {
        public static void AddMedicationEndpoint(this IEndpointRouteBuilder app)
        {
            // Nested under the patient, like careplan and visit: an order is always an
            // order for someone. The care home and the administering staff member come
            // off the caller's token, so the group needs an identity.
            var medication = app.MapGroup("/api/patient/{patientId:Guid}/medication")
                .WithTags("MedicationOrder")
                .RequireAuthorization();

            medication.MapPost("/", CreateMedicationOrder);
            medication.MapGet("/due", GetDueMedicationForDay);
            medication.MapGet("/outstanding", GetOutstandingMedicationForDay);
            medication.MapGet("/{orderId:Guid}", GetMedicationOrder);
            medication.MapPut("/{orderId:Guid}/status", UpdateMedicationOrderStatus);
            medication.MapPost("/{orderId:Guid}/administrations", RecordMedicationAministration);
        }

        public static async Task<IResult> CreateMedicationOrder(
            Guid patientId, CreateMedicationOrderRequest request,
            IMedicationService medicationService, CancellationToken token)
        {
            var response = await medicationService.CreateMedicationOrderAsync(patientId, request, token);
            return response.IsFailure
                ? response.ToProblem()
                : TypedResults.Created($"/api/patient/{patientId}/medication/{response.Value.OrderId}", response.Value);
        }

        public static async Task<IResult> GetDueMedicationForDay(
            Guid patientId, DateOnly day,
            IMedicationService medicationService, CancellationToken token)
        {
            var response = await medicationService.GetDueForDay(patientId, day, token);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult> GetOutstandingMedicationForDay(
            Guid patientId, DateOnly day,
            IMedicationService medicationService, CancellationToken token)
        {
            var response = await medicationService.GetOutstandingForDay(patientId, day, token);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult> GetMedicationOrder(
            Guid orderId, IMedicationService medicationService, CancellationToken token)
        {
            var response = await medicationService.GetMedicationOrder(orderId, token);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult> UpdateMedicationOrderStatus(
            Guid orderId, UpdateMedicationOrderStatusRequest request,
            IMedicationService medicationService, CancellationToken token)
        {
            var response = await medicationService.UpdateMedicationOrderStatusAsync(orderId, request.Status, token);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok();
        }

        // The staff member signing the dose off is the caller, never a value the
        // client gets to choose.
        public static async Task<IResult> RecordMedicationAministration(
            Guid orderId, RecordAdministrationRequest request,
            IMedicationService medicationService, ICurrentUser currentUser, CancellationToken token)
        {
            var response = await medicationService.RecordAministration(
                orderId, request.ScheduledFor, request.Outcome, currentUser.UserId, token, request.Notes);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok(response.Value);
        }
    }
}
