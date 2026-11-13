using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Outbox.Application.Contract;
using Outbox.Domain;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Outbox.Application.Services
{
    public class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
        : BackgroundService
    {
        private static readonly TimeSpan Idle = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan AfterError = TimeSpan.FromSeconds(30);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // A scope per iteration: its DbContext and connection must not
                    // be held across the lifetime of the service.
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotification>();

                    var message = await outboxService.ClaimNextMessagePendingAsync(stoppingToken);
                    if (message.IsFailure)
                    {
                        await Task.Delay(Idle, stoppingToken);
                        continue;
                    }

                    // A send that throws (SMTP down, bad address, bad payload) is a
                    // failure of this message, so it is recorded as Failed and counts
                    // toward MaxAttempts. Left to the outer catch, the status never
                    // changed, RetryCount never moved, and the message was retried on
                    // every lease expiry forever. Shutdown is not a failure: the lease
                    // simply expires and the message is picked up again.
                    Result result;
                    try
                    {
                        result = await notificationService.ProcessEmailNotificationAsync(
                            message.Value.Payload, message.Value.PayloadName, stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning(ex, "Sending outbox message {MessageId} ({PayloadName}) failed.",
                            message.Value.Id, message.Value.PayloadName);
                        result = Result.Failure(new Error("Outbox.SendFailed", ex.Message, ErrorKind.Validation));
                    }

                    var status = result.IsSuccess ? nameof(OutboxStatus.Done) : nameof(OutboxStatus.Failed);
                    await outboxService.UpdateMessageStatus(status, message.Value.Id, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;                      // ordinary shutdown, not a failure
                }
                catch (Exception ex)
                {
                    // Back off before retrying. Without this, a persistent failure --
                    // database down, wrong table name -- spins this loop as fast as
                    // the CPU allows and floods the log. Swallowing it keeps the API
                    // up: an unhandled exception here stops the whole host.
                    logger.LogError(ex, "Outbox drain failed; retrying in {Delay}.", AfterError);
                    try { await Task.Delay(AfterError, stoppingToken); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
    }
}
