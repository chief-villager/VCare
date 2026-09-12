using CarePlans.Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VCare.Modules.CarePlans.Application.Abstractions;
using VCare.Modules.CarePlans.Application.Services;
using VCare.Modules.CarePlans.Infrastructure.Persistence;
using VCare.Modules.CarePlans.Infrastructure.Repositories;

namespace VCare.Modules.CarePlans;

public static class CarePlansModule
{
    public static IServiceCollection AddCarePlansModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CarePlanDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CarePlanDbContext.Schema)));

        // The repository owns the save. IUnitOfWork is registered unkeyed by
        // PatientsModule, so registering it here would win for every consumer.
        services.AddScoped<ICarePlanRepository, CarePlanRepository>();
        services.AddScoped<ICarePlanService, CarePlanService>();

        return services;
    }
}
