using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VCare.SharedKernel.Auditing;

namespace VCare.SharedKernel.Persistence;

public static class PersistenceSetup
{
    /// <summary>The interceptors every module's DbContext adds.</summary>
    public static IServiceCollection AddVCarePersistence(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<CareHomeSessionContext>();
        return services;
    }

    /// <summary>
    /// Audits each save in the same transaction, and tells SQL Server the caller's
    /// care home on each connection, for row-level security.
    /// </summary>
    public static DbContextOptionsBuilder UseVCareInterceptors(this DbContextOptionsBuilder options, IServiceProvider services) =>
        options.AddInterceptors(
            services.GetRequiredService<AuditSaveChangesInterceptor>(),
            services.GetRequiredService<CareHomeSessionContext>());
}
