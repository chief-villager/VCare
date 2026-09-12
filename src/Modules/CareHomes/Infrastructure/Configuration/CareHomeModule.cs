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
            services.AddDbContext<CareHomeDbContext>(Options =>
            {
                Options.
                    UseSqlServer(configuration.GetConnectionString("Default"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CareHomeDbContext.SchemaName));
            });

            services.AddScoped<ICareHomeRepository, CareHomeRepository>();
            services.AddScoped<ICareHomeService, CareHomeService>();

            return services;
        }
    }
}
