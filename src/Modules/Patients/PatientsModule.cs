using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Services;
using VCare.Modules.Patients.Infrastructure.Persistence;
using VCare.Modules.Patients.Infrastructure.Repositories;
using VCare.Modules.Patients.Presentation;

namespace VCare.Modules.Patients;

public static class PatientsModule
{
    public static IServiceCollection AddPatientsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PatientsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", PatientsDbContext.Schema)));

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<PatientService>();

        return services;
    }

    public static IEndpointRouteBuilder MapPatientsEndpoints(this IEndpointRouteBuilder app)
    {
        PatientsEndpoints.MapEndpoints(app);
        return app;
    }
}
