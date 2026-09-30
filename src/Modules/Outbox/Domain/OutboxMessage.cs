using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Outbox.Domain
{
    internal enum OutboxStatus
    {
         Pending,
         Done,
         Failed
    }
  
    internal sealed class OutboxMessage 
    {
        private const int MaxAttempts = 5;
        public Guid Id { get; private set; }

        public string EventType { get; private set; } = null!;

        public string Payload { get; private set; } = null!;
        public string PayloadName {get; private set;}= null!;

        public DateTime CreatedAt { get; private set; }

        public DateTime? ProcessedAt { get; private set; }

        public int RetryCount { get; private set; }

        public DateTime? NextAttemptAt {get; private set;}
        public OutboxStatus Status { get; private set; }

        private OutboxMessage()
        {
            
        }

        public static Result<OutboxMessage>Create(string eventType, IOutboxPayload payload, string payloadName)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                return Result.Failure<OutboxMessage>(new VCare.SharedKernel.Results.Error("400", "EventType is required", ErrorKind.Validation));
            }
            if (string.IsNullOrWhiteSpace(payloadName))
            {
                return Result.Failure<OutboxMessage>(new VCare.SharedKernel.Results.Error("400", "payload Name is required", ErrorKind.Validation));
            }
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType =  eventType,
                Payload = JsonSerializer.Serialize(payload),
                CreatedAt = DateTime.UtcNow,
                Status = OutboxStatus.Pending,
                PayloadName = payloadName

            };
            return Result.Success(message);

        }

        public Result UpdateStatus(string currentStatus)
        {
            if (string.IsNullOrWhiteSpace(currentStatus))
            {
                return Result.Failure(new VCare.SharedKernel.Results.Error("400","Current message status is required"));
            }
            if (Status == OutboxStatus.Done)
            {
               return Result.Failure
               (new VCare.SharedKernel.Results.Error("400","Cannot Update status for message with status outboxstatus.done "));
            }
            if (Status == OutboxStatus.Failed && currentStatus == nameof(OutboxStatus.Failed))
            {
                               
                return Result.Failure
                (new VCare.SharedKernel.Results.Error("403","Cannot Update status from failed to pending ", ErrorKind.Forbidden));

            }
           
            Status = ReturnStatus(currentStatus).Value;
            if (Status == OutboxStatus.Failed && RetryCount < MaxAttempts)
            {
                RetryCount++;
                NextAttemptAt = DateTime.UtcNow.AddMinutes(3);
            }
            return Result.Success();
        }

        private static Result<OutboxStatus> ReturnStatus (string status)
        {
            return status switch
            {
                nameof(OutboxStatus.Done) =>Result.Success(OutboxStatus.Done),
                nameof(OutboxStatus.Failed) =>Result.Success(OutboxStatus.Failed),
                _ => Result.Failure<OutboxStatus>(new VCare.SharedKernel.Results.Error("400","order status not available"))
            };
        }


    }
}