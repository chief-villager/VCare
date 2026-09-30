using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application;
using Notifications.Domain.Entity;
using VCare.SharedKernel.Abstractions;

namespace Notifications
{
    public static class NotificationModuleConfig
    {
        public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<SmtpOptions>()
                .Bind(configuration.GetSection("Smtp"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddScoped<INotification, NotificationService>();
            return services;
        }
    }
}