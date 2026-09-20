using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using src.Modules.CareHomes.Application.Contract;
using src.Modules.CareHomes.Application.Contract.Dto;

namespace src.Modules.CareHomes.Presentation
{
    public static class CareHomeEndpoints
    {
        public static void AddCareHomeEndpoint(this IEndpointRouteBuilder app)
        {
            // Registration lives in the host: a care home is created together with
            // its first admin, which needs the Staffs module too.
            var carehome = app.MapGroup("/api/carehome").WithTags("CareHome").RequireAuthorization();
            carehome.MapPut("/{careHomeId:Guid}",UpdateCareHome);
        }

        public static async Task<IResult>UpdateCareHome(Guid careHomeId, UpdateCareHomeRequest request, 
            ICareHomeService careHomeService, CancellationToken cancellationToken)
        {
            var response = await careHomeService.UpdateCareHomeAsync(careHomeId, request, cancellationToken);
            return response.IsFailure ? TypedResults.BadRequest(response.Error) : TypedResults.Ok();
        }
    }
}