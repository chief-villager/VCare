using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Services;

namespace Staffs.Infrastructure.Persistence
{
    internal class StaffDbContext(DbContextOptions<StaffDbContext> options, ICurrentUser currentUser,
     IDomainEventDispatcher domainEventDispatcher, IOutboxParticipant outboxParticipant)
        : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IUnitOfWork
    {
        public static string schemaName = "Auth";
        public DbSet<Staff> Staffs { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens {get; set;}

        // The caller's care home. Referenced by the query filter so EF re-evaluates
        // it per request; a DbContext is scoped, so the care home is fixed for its lifetime.
        private CareHomeId CurrentCareHome => new(currentUser.CareHomeId);

        public async Task<int> CommitandSaveAsync(CancellationToken cancellationToken)
        {
            
            await using var transaction = await  Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await DispatchDomainEventsAsync(cancellationToken);
                 // acceptAllChangesOnSuccess: false -- the transaction can still roll back
                 // below, and the tracker must not pretend these rows already landed.
                var written = await SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);
                await outboxParticipant.FlushAsync(transaction.GetDbTransaction(), cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Only now does the tracked state match the database.
                ChangeTracker.AcceptAllChanges();
                return written;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            
           
          
        }
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            builder.Properties<StaffId>().HaveConversion<StaffTypedIdConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(schemaName);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StaffDbContext).Assembly);

            // Care home isolation: a caller can only ever read staff from their own care home,
            // regardless of role. Writes are stamped with the care home at creation time.
            modelBuilder.Entity<Staff>().HasQueryFilter(s => s.CareHomeId == CurrentCareHome);
            base.OnModelCreating(modelBuilder);
        }

        private async Task DispatchDomainEventsAsync(CancellationToken ct)
        {
            // Loop so that events raised by handlers (rare) are also dispatched.
            while (true)
            {
                var aggregates = ChangeTracker
                    .Entries<IHasDomainEvents>()
                    .Where(e => e.Entity.DomainEvents.Count > 0)
                    .Select(e => e.Entity)
                    .ToList();
                if (aggregates.Count == 0)
                    break;

                var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
                foreach (var aggregate in aggregates)
                    aggregate.ClearDomainEvents();

                await domainEventDispatcher.DispatchAsync(domainEvents, ct);
            }
        }

    }
}