using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;

namespace Staffs.Infrastructure.Persistence
{
    internal class StaffDbContext(DbContextOptions<StaffDbContext> options) : DbContext(options)
    {
        public static string schemaName = "Staffs";
        public DbSet<Staffs.Domain.Entity.Staff> Staffs { get; set; } = null!;

       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(StaffDbContext).Assembly);

        }
        
    }
}