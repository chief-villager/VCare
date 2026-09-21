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
            patient.MapPost("/",RegisterPatient);
            patient.MapGet("/",ListPatients);
            patient.MapGet("/{patientId:Guid}",GetPatient);
            patient.MapPut("/{patientId:Guid}",UpdatePatient);

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
