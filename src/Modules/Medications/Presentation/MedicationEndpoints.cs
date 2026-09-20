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

namespace Medications.Presentation
{
    public static class MedicationEndpoints
    {
        public static void AddMedicationEndpoint(this IEndpointRouteBuilder app)
        {
           var Medication = app.MapGroup("/api/patient/{patientId:Guid}/Medication").WithTags("MedicationOrder");
           Medication.MapPost("/",CreateMedicationOrder);
           Medication.MapGet("/outstanding",GetOutstandingMedicationForDay);
           Medication.MapGet("/update",UpdateMedicationOrderStatus);
           Medication.MapGet("/record",RecordMedicationAministration);

        }

        public static async Task<IResult>CreateMedicationOrder(
            Guid patientId, CreateMedicationOrderRequest request, 
            IMedicationService medicationService, CancellationToken token)
        {
           var response =  await medicationService.CreateMedicationOrderAsync(patientId,request,token);
           return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.Created(response.Value.OrderId.ToString(), response.Value);
        }

        public static async Task<IResult> GetOutstandingMedicationForDay(
            Guid patientId, DateOnly day,
            IMedicationService medicationService, CancellationToken token)
        {
           var response = await medicationService.GetOutstandingForDay(patientId,day,token);
           return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.Ok(response.Value);
        }

        public static async Task<IResult>UpdateMedicationOrderStatus(Guid medicationOrderId, string status, IMedicationService medicationService, CancellationToken token)
        {
            var response = await medicationService.UpdateMedicationOrderStatusAsync(medicationOrderId,status,token);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.Ok();
        }

        public static async Task<IResult>RecordMedicationAministration(Guid orderId,DateTime scheduledFor, string outcome  
        ,IMedicationService medicationService,ICurrentUser currentUser, CancellationToken token, string? notes = null)
        {
            var response = await medicationService.RecordAministration(orderId,scheduledFor,outcome,currentUser.UserId,token);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.Ok();
        }

        public static async Task<IResult>GetMedicationOrder(Guid orderId,IMedicationService medicationService, CancellationToken token)
        {
            var response = await medicationService.GetMedicationOrder(orderId,token);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.Ok(response.Value);
        }
   }
}  