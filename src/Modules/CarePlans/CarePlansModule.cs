using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CarePlans.Application.Abstract;
using CarePlans.Infrastructure.Repository;
using Vcare.Modules.CarePlans.Infrastructure.Persistence;
using VCare.Modules.CarePlans.Application.Services;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans
{
    public static class CarePlanModule
    {
        public static IServiceCollection AddCarePlansModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CarePlanDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("Default"),
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CarePlanDbContext.Schema)));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CarePlanDbContext>());
            services.AddScoped<ICarePlanRepository, CarePlanRepository>();
            services.AddScoped<CarePlanService>();

            return services;
        }
    }
}