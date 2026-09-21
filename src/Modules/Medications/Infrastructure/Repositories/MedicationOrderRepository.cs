using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Medications.Application.Abstracts;
using Medications.Domain.Entities;
using Medications.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Medications.Infrastructure.Repositories
{
    internal sealed class MedicationOrderRepository(MedicationDbContext medicationOrderDb) : IMedicationOrderRepository
    {
        private readonly MedicationDbContext _medicationOrderDb = medicationOrderDb;

        /// <summary>
        /// returns an enumerable list of medication order for 
        /// a resident begining from fromdate to to.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MedicationOrder>> ActiveBetween(Guid patientId, DateOnly from, DateOnly to)
        {
            var patient = new PatientId(patientId);
            var listofOrder = await _medicationOrderDb.MedicationOrders.Include(o => o.Schedule)                      // load the schedule too
                .Where(o => o.PatientId == patient
                   && o.StartDate <= to                  // started on/before month end
                   && (o.EndDate == null || o.EndDate >= from))  // and not ended before month start
                    .ToListAsync();
            return listofOrder;
        }

        public async Task AddAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken)
        {
            await _medicationOrderDb.MedicationOrders.AddAsync(medicationOrder, cancellationToken);
            
        }

        public async Task<List<MedicationOrder>> GetAllMedicationBelongingToAUserAsync(Expression<Func<MedicationOrder, bool>> expression, CancellationToken cancellationToken)
        {
            var medicationOrders = await _medicationOrderDb.MedicationOrders.Where(expression).ToListAsync(cancellationToken);
            return medicationOrders;
        }

        // Null when there is no such order, and equally when it belongs to another
        // care home: the query filter hides it, and the caller reports "not found"
        // either way rather than confirming it exists elsewhere.
        public async Task<MedicationOrder?> GetAsync(Guid Id, CancellationToken cancellationToken)
        {
            return await _medicationOrderDb.MedicationOrders
                .FindAsync(new object?[] { new MedicationOrderId(Id) }, cancellationToken: cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _medicationOrderDb.SaveChangesAsync(cancellationToken);
        }

        public  async Task UpdateAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken)
        {
            _medicationOrderDb.MedicationOrders.Update(medicationOrder);            
        }
    }
}