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
        services.AddDbContext<VisitationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", VisitationDbContext.Schema)));

        // The repository owns the save. IUnitOfWork is registered unkeyed by
        // PatientsModule, so registering it here would win for every consumer.
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IVisitationService, VisitationService>();

        return services;
    }
}
