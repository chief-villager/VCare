using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Infrastructure.Persistence
{
    internal class StaffDbContext(DbContextOptions<StaffDbContext> options, ICurrentUser currentUser)
        : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
    {
        public static string schemaName = "Staffs";
        public DbSet<Staff> Staffs { get; set; } = null!;

        // The caller's care home. Referenced by the query filter so EF re-evaluates
        // it per request; a DbContext is scoped, so the care home is fixed for its lifetime.
        private CareHomeId CurrentCareHome => new(currentUser.CareHomeId);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StaffDbContext).Assembly);

            // Care home isolation: a caller can only ever read staff from their own care home,
            // regardless of role. Writes are stamped with the care home at creation time.
            modelBuilder.Entity<Staff>().HasQueryFilter(s => s.CareHomeId == CurrentCareHome);
        }

    }
}