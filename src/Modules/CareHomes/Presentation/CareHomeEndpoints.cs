using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using src.Modules.CareHomes.Application.Contract;
using src.Modules.CareHomes.Application.Contract.Dto;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace src.Modules.CareHomes.Presentation
{
    public static class CareHomeEndpoints
    {
        public static void AddCareHomeEndpoint(this IEndpointRouteBuilder app)
        {
            // Registration lives in the host: a care home is created together with
            // its first admin, which needs the Staffs module too.
            var carehome = app.MapGroup("/api/carehome").WithTags("CareHome").RequireAuthorization();
            carehome.MapPut("/me",UpdateCareHome);
        }

        public static async Task<IResult>UpdateCareHome(UpdateCareHomeRequest request, 
            ICareHomeService careHomeService,ICurrentUser currentUser, CancellationToken cancellationToken)
        {
            var response = await careHomeService.UpdateCareHomeAsync(currentUser.CareHomeId, request, cancellationToken);
            return response.IsFailure ? response.ToProblem() : TypedResults.Ok();
        }
    }
}