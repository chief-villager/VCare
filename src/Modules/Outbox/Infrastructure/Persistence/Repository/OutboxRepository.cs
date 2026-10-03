using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Outbox.Application.Contract;
using Outbox.Domain;

namespace Outbox.Infrastructure.Persistence.Repository
{
    internal class OutboxRepository(OutboxDbContext outboxDbContext) : IOutboxRepository
    {
        public async Task<OutboxMessage?> ClaimNextPendingAsync(
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await outboxDbContext.Database.BeginTransactionAsync(cancellationToken);

            var message = await outboxDbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT TOP (1) *
                    FROM Outboxes WITH (ROWLOCK, UPDLOCK, READPAST)
                    WHERE Status IN ({(int)OutboxStatus.Pending}, {(int)OutboxStatus.Failed})
                    AND (NextAttemptAt IS NULL OR NextAttemptAt <= GETUTCDATE())
                    ORDER BY CreatedAt
                    """)
                .FirstOrDefaultAsync(cancellationToken);
            transaction.Commit();

            return message;
        }
      

        public async Task<OutboxMessage?> GetOutboxMessageAsync(Guid Id, CancellationToken cancellationToken)
        {
            return await outboxDbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == Id, cancellationToken);
        }

        async Task IOutboxRepository.AddAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
        {
            await outboxDbContext.AddAsync(outboxMessage, cancellationToken);  
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await outboxDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}