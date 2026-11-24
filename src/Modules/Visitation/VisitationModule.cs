using VCare.SharedKernel.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VCare.Modules.Visitation.Application.Abstractions;
using VCare.Modules.Visitation.Application.Services;
using VCare.Modules.Visitation.Infrastructure.Persistence;
using VCare.Modules.Visitation.Infrastructure.Repositories;
using Visitation.Application.Services.Interfaces;

namespace VCare.Modules.Visitation;

public static class VisitationModule
{
    public static IServiceCollection AddVisitationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<VisitationDbContext>((sp, options) =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", VisitationDbContext.Schema))
                .UseVCareInterceptors(sp));

        // The repository owns the save. IUnitOfWork is registered unkeyed by
        // PatientsModule, so registering it here would win for every consumer.
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IVisitationService, VisitationService>();
        // Check-in and check-out are timed here, not by the client.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
