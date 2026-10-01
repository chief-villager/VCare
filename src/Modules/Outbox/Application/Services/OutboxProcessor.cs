using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outbox.Application.Contract;
using Outbox.Domain;
using VCare.SharedKernel.Abstractions;

namespace Outbox.Application.Services
{
    public class OutboxProcessor(IServiceScopeFactory scopeFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = scopeFactory.CreateAsyncScope();
                var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotification>();
                var message = await outboxService.ClaimNextMessagePendingAsync(stoppingToken);
                if (message.IsFailure)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }
                var result = await notificationService.ProcessEmailNotificationAsync
                            (message.Value.Payload,message.Value.PayloadName, stoppingToken);
                if (result.IsSuccess)
                {
                    await outboxService.UpdateMessageStatus(nameof(OutboxStatus.Done),message.Value.Id,stoppingToken);
                }
                else
                {
                     await outboxService.UpdateMessageStatus(nameof(OutboxStatus.Failed),message.Value.Id,stoppingToken);
                }
                    
            }
           

           
        }
    }
}