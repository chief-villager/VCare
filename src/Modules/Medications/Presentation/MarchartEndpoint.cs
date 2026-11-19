using Medications.Application.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using VCare.SharedKernel.Results;

namespace Medications.Presentation
{
    public static class MarchartEndpoint
    {
        public static void AddMarchartEndpoint(this IEndpointRouteBuilder app)
        {
            var marchart = app.MapGroup("api/patient/{patientId:Guid}/Marchart").WithTags("Marchart").RequireAuthorization();
            marchart.MapGet("/", GetMarChart).RequireAuthorization("Admin_Role");

        }

        public static async Task<IResult> GetMarChart(Guid patientId, IMarChartService marChartService,
            [FromQuery] int year, [FromQuery] int month, CancellationToken token)
        {
            var marChart = await marChartService.BuildChart(patientId, year, month, token);
            return marChart.IsFailure ? marChart.ToProblem() : TypedResults.Ok(marChart.Value);
        }
    }
}
