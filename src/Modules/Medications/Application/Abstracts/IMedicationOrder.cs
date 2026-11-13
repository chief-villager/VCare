using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Medications.Domain.Entities;

namespace Medications.Application.Abstracts
{
    internal interface IMedicationOrderRepository
    {
        Task<MedicationOrder?> GetAsync(Guid Id, CancellationToken cancellationToken);
        Task UpdateAsync (MedicationOrder medicationOrder, CancellationToken cancellationToken);
        Task AddAsync( MedicationOrder medicationOrder, CancellationToken cancellationToken);
        Task<List<MedicationOrder>> GetAllMedicationBelongingToAUserAsync(Expression<Func<MedicationOrder, bool>> expression, CancellationToken cancellationToken);
        Task<IEnumerable<MedicationOrder>> ActiveBetween(Guid patientId, DateOnly from, DateOnly to);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Active scheduled (non-PRN) orders live at any point between the
        /// two days, in every care home. For the missed-dose recorder only: it runs
        /// with no signed-in user, so the care-home filter would match nothing.</summary>
        Task<List<MedicationOrder>> ActiveScheduledForAllCareHomesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    };
}