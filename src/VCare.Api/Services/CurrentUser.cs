using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using VCare.SharedKernel.Abstractions;

namespace VCare.Api.Services
{
    internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public const string CareHomeClaim = "care_home_id";

        // A route open to anyone (sign-in, registration, password reset) works the
        // same with or without a token, so a token sent to it is ignored. Otherwise a
        // signed-in admin registering another care home would be held to their own,
        // and the database would refuse to create it.
        private ClaimsPrincipal? Principal =>
            accessor.HttpContext is { } context
            && context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is null
                ? context.User
                : null;

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
