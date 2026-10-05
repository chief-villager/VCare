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
        // How long a claimed message stays invisible to other workers. Must
        // comfortably outlast the slowest send: SmtpClient's default timeout is
        // 100 seconds, so a worker that hangs still finishes well inside this.
        private const int LeaseMinutes = 5;

        public async Task<OutboxMessage?> ClaimNextPendingAsync(
            CancellationToken cancellationToken)
        {
            // Claiming is a WRITE, not a lock. Pushing NextAttemptAt into the
            // future makes the row invisible to the claim filter below, which is
            // what stops a second worker taking the same message -- a lock would
            // have been released the moment this statement finished. If this
            // worker then dies, the lease simply expires and someone else picks
            // the message up: no sweeper, no rows stuck in a "processing" state.
            //
            // One statement, so it is atomic on its own and needs no transaction.
            // The CTE exists because UPDATE TOP (1) cannot carry an ORDER BY, and
            // the queue is FIFO. Raw SQL does not pick up HasDefaultSchema, so the
            // table is spelled out and must stay in step with schemaName.
            var claimed = await outboxDbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    WITH next AS (
                        SELECT TOP (1) *
                        FROM Outbox.OutboxMessages WITH (ROWLOCK, UPDLOCK, READPAST)
                        WHERE Status IN ({(int)OutboxStatus.Pending}, {(int)OutboxStatus.Failed})
                        AND (NextAttemptAt IS NULL OR NextAttemptAt <= GETUTCDATE())
                        ORDER BY CreatedAt
                    )
                    UPDATE next
                    SET NextAttemptAt = DATEADD(minute, {LeaseMinutes}, GETUTCDATE())
                    OUTPUT inserted.*
                    """)
                .ToListAsync(cancellationToken);

            // ToList rather than FirstOrDefault: composing a LINQ operator onto a
            // FromSql wraps it in "SELECT TOP(1) * FROM ( ... )", which is legal
            // around a SELECT and not around an UPDATE.
            return claimed.FirstOrDefault();
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