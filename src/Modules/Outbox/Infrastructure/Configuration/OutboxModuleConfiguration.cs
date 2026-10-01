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
using VCare.SharedKernel.Abstractions;
using System.Data.Common;

namespace Outbox.Infrastructure.Configuration
{
    public static class OutboxModuleConfiguration
    {
        public static IServiceCollection AddOutboxModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Same request-scoped connection as the writing module's context, so an
            // outbox row can join that module's transaction. The background processor
            // gets its own scope, hence its own connection, and is unaffected.
            services.AddDbContext<OutboxDbContext>((sp, options) =>
            {
                options.UseSqlServer(sp.GetRequiredService<DbConnection>(),
                    Options => Options.MigrationsHistoryTable("Outbox",OutboxDbContext.schemaName));
            });
            services.AddScoped<IOutboxRepository, OutboxRepository>();

            // Both ends of the queue: the processor drains it through IOutboxService,
            // the other modules enqueue through IOutboxWriter. IOutboxService derives
            // from IOutboxWriter, but DI resolves by exact type and will not walk to
            // the base interface, so the base still needs its own registration --
            // forwarded, so both ends get the one instance.
            services.AddScoped<IOutboxService, OutboxService>();
            services.AddScoped<IOutboxWriter>(sp => sp.GetRequiredService<IOutboxService>());
            services.AddHostedService<OutboxProcessor>();
            services.AddScoped<IOutboxParticipant, OutboxParticipant>();
            return services;
        }
    }
}