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
         Failed,
         // Out of retries. Excluded from the claim query, so an undeliverable
         // message stops being picked up instead of sitting at the head of the
         // queue forever. Appended last: the stored values are ints.
         Abandoned
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

        // newStatus is where the message is going, not where it is. Callers pass
        // nameof(OutboxStatus.Done) or nameof(OutboxStatus.Failed).
        public Result UpdateStatus(string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                return Result.Failure(new VCare.SharedKernel.Results.Error("400", "A status is required."));
            }
            if (newStatus != nameof(OutboxStatus.Done) && newStatus != nameof(OutboxStatus.Failed))
            {
                return Result.Failure(new VCare.SharedKernel.Results.Error(
                    "400", "A message can only move to Done or Failed."));
            }
            if (Status is OutboxStatus.Done or OutboxStatus.Abandoned)
            {
                return Result.Failure(new VCare.SharedKernel.Results.Error(
                    "400", $"A message that is already {Status} cannot change status."));
            }

            if (newStatus == nameof(OutboxStatus.Done))
            {
                Status = OutboxStatus.Done;
                ProcessedAt = DateTime.UtcNow;
                NextAttemptAt = null;
                return Result.Success();
            }

            // Failed. Counted once per failure, wherever the message started, so
            // MaxAttempts actually bounds the number of sends.
            RetryCount++;
            if (RetryCount >= MaxAttempts)
            {
                Status = OutboxStatus.Abandoned;
                NextAttemptAt = null;
                return Result.Success();
            }

            Status = OutboxStatus.Failed;
            NextAttemptAt = DateTime.UtcNow.AddMinutes(3);
            return Result.Success();
        }
    }
}