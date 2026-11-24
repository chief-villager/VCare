using VCare.SharedKernel.Persistence;
using Medications.Application.Abstracts;
using Medications.Application.Services;
using Medications.Application.Services.Interfaces;
using Medications.Infrastructure.Persistence;
using Medications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Medications;

public static class MedicationsModule
{
    public static IServiceCollection AddMedicationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MedicationDbContext>((sp, options) =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", MedicationDbContext.Schema))
                .UseVCareInterceptors(sp));

        // The repositories own the save. IUnitOfWork is registered unkeyed by
        // PatientsModule, so resolving it here would hand this module the Patients
        // DbContext and silently drop every medication write.
        services.AddScoped<IMedicationOrderRepository, MedicationOrderRepository>();
        services.AddScoped<IMedicalAdministrationRepository, MedicationAdministrationRepository>();
        services.AddScoped<IOutcomeRepository, OutComeRepisitory>();
        services.AddScoped<IMarChartService, MarChartService>();

        // One clock for sign-off, the MAR chart and the missed-dose recorder, so
        // they agree on whether a dose is due, and tests can set the time.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ScheduleExpander>();
        services.AddScoped<IMedicationService, MedicationService>();

        // Stores "Missed" for doses whose grace hour ran out unsigned (DoseWindow).
        services.AddScoped<MissedDoseRecorder>();

        // Each pass takes a database lock (IJobLock), so with several instances
        // only one does the work. BackgroundJobs:Enabled=false still turns the
        // timer off entirely, for an instance that should never run jobs.
        services.AddScoped<MissedDoseJob>();
        if (configuration.GetValue("BackgroundJobs:Enabled", true))
        {
            services.AddHostedService<MissedDoseBackgroundService>();
        }

        return services;
    }
}
