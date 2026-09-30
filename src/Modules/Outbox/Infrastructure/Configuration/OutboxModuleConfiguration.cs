using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Outbox.Application.Contract;
using Outbox.Application.Services;
using Outbox.Infrastructure.Persistence;
using Outbox.Infrastructure.Persistence.Repository;

namespace Outbox.Infrastructure.Configuration
{
    internal static class OutboxModuleConfiguration
    {
        public static IServiceCollection AddModuleOutboxModuleConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connection = configuration.GetConnectionString("ConnectionStrings");
            services.AddDbContext<OutboxDbContext>(Options =>
            {
                Options.UseSqlServer(connection, Options => Options.MigrationsHistoryTable("Outbox",OutboxDbContext.schemaName));
            });
            services.AddScoped<IOutboxService, OutboxService>();
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddHostedService<OutboxProcessor>();
            return services;
        }
    }
}