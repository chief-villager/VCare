using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Staffs.Infrastructure.Persistence;

namespace Staffs.Infrastructure
{
    public static class StaffModule
    {
       
        public static IServiceCollection AddStaffModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Staffs.Infrastructure.Persistence.StaffDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("Default"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", StaffDbContext.schemaName)));

            services.AddScoped<Staffs.Application.Abstraction.IStaffRepository, Staffs.Infrastructure.Repositories.StaffRepository>();

            return services;
        }
    }
}