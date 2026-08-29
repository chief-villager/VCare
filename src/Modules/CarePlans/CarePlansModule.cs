using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vcare.Modules.CarePlans.Infrastructure.Persistence;

namespace VCare.Modules.CarePlans
{
    public static class CarePlanModule
    {
        public static IServiceCollection AddCarePlansModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register your CarePlanService and any other dependencies here
            // Example: services.AddScoped<ICarePlanService, CarePlanServiceImplementation>();
            services.AddDbContext<CarePlanDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("Default"),
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CarePlanDbContext.Schema)));

            return services;
        }   
    }
}