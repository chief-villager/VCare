using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Outbox.Domain;

namespace Outbox.Application.Contract
{
    internal interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken);
        Task<OutboxMessage?> ClaimNextPendingAsync(CancellationToken cancellationToken);
        Task<OutboxMessage?> GetOutboxMessageAsync(Guid Id, CancellationToken cancellationToken); 
    }
}