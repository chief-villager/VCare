using System;

namespace VCare.SharedKernel.Abstractions
{
    /// <summary>
    /// Ambient information about the caller, resolved per request from the
    /// authenticated principal. Modules depend on this abstraction so they can
    /// scope work to the caller's care home without referencing ASP.NET.
    /// </summary>
    public interface ICurrentUser
    {
        Guid UserId { get; }

        /// <summary>The business the caller belongs to.</summary>
        Guid CareHomeId { get; }

        bool IsAuthenticated { get; }

        bool IsInRole(string role);
    }
}
