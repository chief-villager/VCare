using VCare.SharedKernel.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using src.Modules.CareHomes.Application.Contract;
using src.Modules.CareHomes.Application.Service;
using src.Modules.CareHomes.Infrastructure.Persistence;
using src.Modules.CareHomes.Infrastructure.Repository;
using VCare.SharedKernel.Abstractions;

namespace src.Modules.CareHomes.Infrastructure.Configuration
{
    public static class CareHomeModule
    {
        public static IServiceCollection  AddCareHomeModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CareHomeDbContext>((sp, options) =>
                options.UseSqlServer(configuration.GetConnectionString("Default"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CareHomeDbContext.SchemaName))
                    .UseVCareInterceptors(sp));

            services.AddScoped<ICareHomeRepository, CareHomeRepository>();
            services.AddScoped<ICareHomeService, CareHomeService>();

            // The wall clock in each home, for every module that asks whether a dose
            // or an absence time has come. TimeProvider so tests can set the time.
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<ICareHomeClock, CareHomeClock>();

            return services;
        }
    }
}
