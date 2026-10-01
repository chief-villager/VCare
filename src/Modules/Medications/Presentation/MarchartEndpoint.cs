using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Application.Services;
using Medications.Application.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Medications.Presentation
{
    public static class MarchartEndpoint
    {
        public static void AddMarchartEndpoint(this IEndpointRouteBuilder app)
        {
            var marchart = app.MapGroup("api/patient/{patientId}/Marchart").WithTags("Marchart");
            marchart.AddMarchartEndpoint();

        }

        public static async Task<IResult> GetMarChart(Guid patientId,IMarChartService marChartService,int year, int month)
        {
            var marChart = await marChartService.BuildChart(patientId,year,month);
            return marChart == null ? TypedResults.Ok("There is no recorded medication for this month")
                : TypedResults.Ok(marChart);
            
            
        }
    }
}