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
        Task<MedicationOrder> GetAsync(Guid Id, CancellationToken cancellationToken);
        Task UpdateAsync (MedicationOrder medicationOrder, CancellationToken cancellationToken);
        Task AddAsync( MedicationOrder medicationOrder, CancellationToken cancellationToken);
        Task<List<MedicationOrder>> GetAllMedicationBelongingToAUserAsync(Expression<Func<MedicationOrder, bool>> expression, CancellationToken cancellationToken);
        IEnumerable<MedicationOrder> ActiveBetween(Guid patientId, DateOnly from, DateOnly to);

    };
}