using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Patients.Application.Dtos;
using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Dtos;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Patients.Presenstation
{
    public  static class PatientEndpoint
    {
        public static void AddPatientEndpoints(this IEndpointRouteBuilder app)
        {
            // The care home is taken from the caller's token, not the route, so
            // every read below is already scoped to it by the query filter.
            var patient = app.MapGroup("/api/patient").WithTags("Patients").RequireAuthorization();
            patient.MapPost("/",RegisterPatient).RequireAuthorization("Admin_Role");
            patient.MapGet("/",ListPatients).RequireAuthorization("Admin_Or_Carer");
            patient.MapGet("/{patientId:Guid}",GetPatient).RequireAuthorization("Admin_Or_Carer");
            patient.MapPut("/{patientId:Guid}",UpdatePatient).RequireAuthorization("Admin_Role");
            patient.MapPost("/{patientId:Guid}/document",UploadPatientDocument).RequireAuthorization("Admin_Role");
            patient.MapPost("/{patientId:Guid}/profile-image",UploadProfileImageForPatient).RequireAuthorization("Admin_Role");
            patient.MapGet("/{patientId:Guid}/document",GetPatientDocument).RequireAuthorization("Admin_Or_Carer");

        }

        public async static Task<IResult>UploadPatientDocument(Guid patientId, IFormFile formFile, string fileName, IPatientService patientService, CancellationToken cancellationToken)
        {
             var result = await patientService.UploadPatientDocumentAsync(new PatientId(patientId), formFile,fileName,cancellationToken);
             return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { Key = result.Value });
        }
        public async static Task<IResult>UploadProfileImageForPatient(Guid patientId, IFormFile formFile, IPatientService patientService, CancellationToken cancellationToken)
        {
             var result = await patientService.UploadProfileImageForPatientAsync(new PatientId(patientId), formFile,cancellationToken);
             return result.IsFailure ? result.ToProblem() : TypedResults.Ok(new { Key = result.Value });
        }

        public async static Task<IResult>GetPatientDocument(Guid patientId, string key, IPatientService patientService, CancellationToken cancellationToken)
        {
            var result = await patientService.GetPatientDocumentsAsync(new PatientId(patientId), key, cancellationToken);
            return result.IsFailure ? result.ToProblem() : TypedResults.File(result.Value.Content, result.Value.ContentType);
        }


        public async static Task<IResult>RegisterPatient(RegisterPatientRequest request, IPatientService patientService, CancellationToken cancellationToken)
        {
           var response =  await patientService.RegisterAsync(request,cancellationToken);
           return response.IsFailure
               ? response.ToProblem()
               : TypedResults.Created($"/api/patient/{response.Value.Id}", response.Value);

        }

        public async static Task<IResult>ListPatients(IPatientService patientService, CancellationToken cancellationToken)
        {
            var response = await patientService.ListAsync(cancellationToken);
            return TypedResults.Ok(response);
        }

        public async static Task<IResult>GetPatient(Guid patientId, IPatientService patientService, CancellationToken cancellationToken)
        {
            var response = await patientService.GetByIdAsync(patientId,cancellationToken);
            return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
        }

        public async static Task<IResult>UpdatePatient(Guid patientId, UpdatePatientRequest request, 
            IPatientService patientService, CancellationToken cancellationToken)
        {
            var response = await patientService.UpdateAsync(patientId,request,cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.NoContent();
        }
    }
}
