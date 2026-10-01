using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Outbox.Domain;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Outbox.Application.Contract
{
    internal interface IOutboxService : IOutboxWriter
    {
        Task<Result<OutboxMessage>>ClaimNextMessagePendingAsync(CancellationToken cancellationToken);
        Task<Result>UpdateMessageStatus(string status,Guid Id,CancellationToken cancellationToken);
    }
}