using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Application.Services;

namespace VCare.Modules.Patients.Presentation;

internal static class PatientsEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients").WithTags("Patients");

        group.MapGet("/{id:guid}", async (Guid id, PatientService service, CancellationToken ct) =>
        {
            var patient = await service.GetByIdAsync(id, ct);
            return patient is null ? Results.NotFound() : Results.Ok(patient);
        });

        group.MapPost("/", async (RegisterPatientRequest request, PatientService service, CancellationToken ct) =>
        {
            var id = await service.RegisterAsync(request, ct);
            return Results.Created($"/api/patients/{id}", new { id });
        });
    }
}
