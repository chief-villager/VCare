using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Outbox.Domain;

namespace Outbox.Infrastructure.Persistence
{
    internal class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : DbContext(options)
    {
        public static string schemaName = "Outboxes";
        public DbSet<OutboxMessage> OutboxMessages{get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}