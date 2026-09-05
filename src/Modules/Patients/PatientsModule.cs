using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Patients.Application.Services.Interfaces;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Services;
using VCare.Modules.Patients.Infrastructure.Persistence;
using VCare.Modules.Patients.Infrastructure.Repositories;

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
        services.AddScoped<IPatientService, PatientService>();

        return services;
    }
}
