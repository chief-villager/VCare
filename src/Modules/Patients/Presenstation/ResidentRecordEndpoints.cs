using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Application.Services;
using VCare.SharedKernel.Results;

namespace Patients.Presenstation
{
    public static class ResidentRecordEndpoints
    {
        public static void AddResidentRecordEndpoints(this IEndpointRouteBuilder app)
        {
            var patient = app.MapGroup("/api/patient/{patientId:Guid}").WithTags("Patients").RequireAuthorization();

            // Any carer can record an allergy they are told about: a missing allergy
            // is the dangerous mistake. Removing one is for an admin.
            patient.MapGet("/allergies", GetAllergies).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/allergies", RecordAllergy).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/allergies/none", RecordNoKnownAllergies).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/allergies/{allergyId:Guid}/remove", RemoveAllergy).RequireAuthorization("Admin_Role");

            // In hospital or on leave: doses due meanwhile are not counted as missed.
            patient.MapGet("/absences", GetAbsences).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/absences", StartAbsence).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/absences/end", EndAbsence).RequireAuthorization("Admin_Or_Carer");

            // Admission details: NHS number, GP, room, admission date. Each call sets all of them.
            patient.MapPut("/care-details", UpdateCareDetails).RequireAuthorization("Admin_Role");

            patient.MapGet("/weights", GetWeights).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/weights", RecordWeight).RequireAuthorization("Admin_Or_Carer");

            // Leaving the home ends the residency: Medications expects no dose after it.
            patient.MapPost("/discharge", Discharge).RequireAuthorization("Admin_Role");
            patient.MapPost("/death", RecordDeath).RequireAuthorization("Admin_Role");
            patient.MapPost("/readmit", Readmit).RequireAuthorization("Admin_Role");

            // Lasting powers of attorney: who may decide for the resident, not who to call.
            patient.MapGet("/attorneys", GetAttorneys).RequireAuthorization("Admin_Or_Carer");
            patient.MapPost("/attorneys", RecordAttorney).RequireAuthorization("Admin_Role");
            patient.MapPost("/attorneys/{attorneyId:Guid}/remove", RemoveAttorney).RequireAuthorization("Admin_Role");
        }

        private static async Task<IResult> UpdateCareDetails(Guid patientId, UpdateCareDetailsRequest request,
            ResidentRecordService records, CancellationToken token) =>
            NoContentOr(await records.UpdateCareDetailsAsync(patientId, request, token));

        private static async Task<IResult> GetWeights(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.GetWeightsAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(result.Value);
        }

        private static async Task<IResult> RecordWeight(Guid patientId, RecordWeightRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RecordWeightAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { WeightId = result.Value });
        }

        private static async Task<IResult> Discharge(Guid patientId, DischargeRequest request,
            ResidentRecordService records, CancellationToken token) =>
            NoContentOr(await records.DischargeAsync(patientId, request, token));

        private static async Task<IResult> RecordDeath(Guid patientId, RecordDeathRequest request,
            ResidentRecordService records, CancellationToken token) =>
            NoContentOr(await records.RecordDeathAsync(patientId, request, token));

        private static async Task<IResult> Readmit(Guid patientId, ReadmitRequest request,
            ResidentRecordService records, CancellationToken token) =>
            NoContentOr(await records.ReadmitAsync(patientId, request, token));

        private static async Task<IResult> GetAttorneys(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.GetAttorneysAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(result.Value);
        }

        private static async Task<IResult> RecordAttorney(Guid patientId, RecordAttorneyRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RecordAttorneyAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { AttorneyId = result.Value });
        }

        private static async Task<IResult> RemoveAttorney(Guid patientId, Guid attorneyId, RemoveAttorneyRequest request,
            ResidentRecordService records, CancellationToken token) =>
            NoContentOr(await records.RemoveAttorneyAsync(patientId, attorneyId, request, token));

        private static IResult NoContentOr(Result result) => result.IsFailure ? result.ToProblem() : TypedResults.NoContent();

        private static async Task<IResult> GetAllergies(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.GetAllergiesAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(result.Value);
        }

        private static async Task<IResult> RecordAllergy(Guid patientId, RecordAllergyRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RecordAllergyAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { AllergyId = result.Value });
        }

        private static async Task<IResult> RecordNoKnownAllergies(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RecordNoKnownAllergiesAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.NoContent();
        }

        private static async Task<IResult> RemoveAllergy(Guid patientId, Guid allergyId, RemoveAllergyRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.RemoveAllergyAsync(patientId, allergyId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.NoContent();
        }

        private static async Task<IResult> GetAbsences(Guid patientId, ResidentRecordService records, CancellationToken token)
        {
            var result = await records.GetAbsencesAsync(patientId, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(result.Value);
        }

        private static async Task<IResult> StartAbsence(Guid patientId, StartAbsenceRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.StartAbsenceAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { AbsenceId = result.Value });
        }

        private static async Task<IResult> EndAbsence(Guid patientId, EndAbsenceRequest request,
            ResidentRecordService records, CancellationToken token)
        {
            var result = await records.EndAbsenceAsync(patientId, request, token);
            return result.IsFailure ? result.ToProblem() : TypedResults.NoContent();
        }
    }
}
