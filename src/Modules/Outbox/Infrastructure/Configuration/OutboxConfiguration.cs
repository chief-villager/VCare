using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Outbox.Domain;
using Outbox.Infrastructure.Persistence;

namespace Outbox.Infrastructure.Configuration
{
    internal  class OutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.HasKey(x => x.Id);
            builder.ToTable("OutboxMessages", OutboxDbContext.schemaName);

            // Serves the drain's claim query, in its order: seek the candidate
            // statuses, filter on NextAttemptAt, then read already sorted by
            // CreatedAt so there is no sort. Without it every poll -- one every
            // ten seconds, against a table nothing prunes -- scans the lot.
            // Deliberately not a filtered index: the claim parameterises Status,
            // and the optimiser will not use a filtered index it cannot prove
            // the predicate for at compile time.
            builder.HasIndex(x => new { x.Status, x.NextAttemptAt, x.CreatedAt })
                   .HasDatabaseName("IX_OutboxMessages_Claim");
        }
    }
}