using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Outbox.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Outbox.Application.Services
{
    internal sealed class OutboxParticipant(OutboxDbContext dbContext) : IOutboxParticipant
    {
        public async Task<Result> FlushAsync(DbTransaction transaction, CancellationToken cancellationToken)
        {
            var changes = dbContext.ChangeTracker.HasChanges();
            if (!changes)
            {
                // Nothing was enqueued this request. That is the ordinary case for a
                // write that raises no events, not a failure.
                return Result.Success();
            }

        // Only legal because every module's DbContext shares one scoped
        // DbConnection. A transaction from a different connection is rejected.
            await dbContext.Database.UseTransactionAsync(transaction, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}