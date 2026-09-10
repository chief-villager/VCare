using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VCare.SharedKernel.Abstractions;

namespace VCare.Api.Services
{
    internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public const string CareHomeClaim = "care_home_id";

        private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public Guid UserId =>
            Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : Guid.Empty;

        public Guid CareHomeId =>
            Guid.TryParse(Principal?.FindFirstValue(CareHomeClaim), out var id)
                ? id
                : Guid.Empty;

        public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
    }
}
