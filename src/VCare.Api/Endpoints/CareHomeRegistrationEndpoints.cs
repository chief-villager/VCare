using VCare.Api.Dtos;
using VCare.Api.Services;

namespace VCare.Api.Endpoints
{
    public static class CareHomeRegistrationEndpoints
    {
        public static void AddCareHomeRegistrationEndpoint(this IEndpointRouteBuilder app)
        {
            // Anonymous by necessity: this is how the first account of a care home
            // comes into existence, so there is nobody to authenticate as yet.
            // It lives in the host, not in CareHomes, because it drives two modules.
            var registration = app.MapGroup("/api/carehome").WithTags("CareHome").AllowAnonymous();
            registration.MapPost("/",RegisterCareHome);
        }

        public static async Task<IResult> RegisterCareHome(
            RegisterCareHomeRequest request,
            CareHomeRegistrationService registrationService,
            CancellationToken cancellationToken)
        {
            var response = await registrationService.RegisterAsync(request, cancellationToken);
            return response.IsFailure
                ? TypedResults.BadRequest(response.Error)
                : TypedResults.Created($"/api/carehome/{response.Value.CareHomeId}", response.Value);
        }
    }
}
