using Medications.Application.Abstracts;
using Medications.Application.Services;
using Medications.Application.Services.Interfaces;
using Medications.Infrastructure.Persistence;
using Medications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Medications;

public static class MedicationsModule
{
    public static IServiceCollection AddMedicationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MedicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", MedicationDbContext.Schema)));

        // The repositories own the save. IUnitOfWork is registered unkeyed by
        // PatientsModule, so resolving it here would hand this module the Patients
        // DbContext and silently drop every medication write.
        services.AddScoped<IMedicationOrderRepository, MedicationOrderRepository>();
        services.AddScoped<IMedicalAdministrationRepository, MedicationAdministrationRepository>();
        services.AddScoped<IOutcomeRepository, OutComeRepisitory>();

        services.AddScoped<ScheduleExpander>();
        services.AddScoped<IMedicationService, MedicationService>();

        return services;
    }
}
