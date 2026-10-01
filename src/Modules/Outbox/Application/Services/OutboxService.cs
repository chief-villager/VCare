using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Outbox.Application.Contract;
using Outbox.Domain;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Outbox.Application.Services
{
    internal class OutboxService(IOutboxRepository outboxRepository) : IOutboxService 
    {   
        
        public async Task<Result> AddOutboxMessageAsync<TPayload>(TPayload payload,string Email, string UserName, 
        string eventType,string url, CancellationToken cancellationToken) where TPayload:IOutboxPayload
        {
            var payloadName = typeof(TPayload).Name;
            var message = OutboxMessage.Create(eventType, payload,payloadName);
            if (message.IsFailure)
            {
                return Result.
                Failure<OutboxMessage>(new Error("404","No pending messages",ErrorKind.NotFound));
            }
            await outboxRepository.AddAsync(message.Value,cancellationToken);
            return Result.Success();
        }

        
        public async Task<Result<OutboxMessage>>ClaimNextMessagePendingAsync(CancellationToken cancellationToken)
        {
           var message = await outboxRepository.ClaimNextPendingAsync(cancellationToken);
           if (message == null)
           {
             return Result.
                Failure<OutboxMessage>(new Error("404","No pending messages",ErrorKind.NotFound));
           }
            return Result.
                    Success(message);
        }


        public async Task<Result>UpdateMessageStatus(string status,Guid Id,CancellationToken cancellationToken)
        {
            var message = await outboxRepository.GetOutboxMessageAsync(Id,cancellationToken);
            if (message == null)
            {
                return Result.Failure(new Error("404","message not found", ErrorKind.NotFound));
            }

            var statusUpdate = message.UpdateStatus(status);
            if (statusUpdate.IsFailure)
            {
                return statusUpdate;
            }

            // The drain side owns its own commit. Without this the processor
            // re-claims the same row on every loop and re-sends the same email.
            await outboxRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}