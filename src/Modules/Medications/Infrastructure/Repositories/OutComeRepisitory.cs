using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Application.Abstracts;
using Medications.Domain.Entities;
using Medications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Medications.Infrastructure.Repositories
{
    internal class OutComeRepisitory(MedicationDbContext dbContext) : IOutcomeRepository
    {
        public async Task<OutcomeCode?>GetOutcomeByName(string name, CancellationToken cancellationToken)
        {
           // Compared in the database: the StringComparison overload of Equals has
           // no SQL translation and throws when the query is executed.
           var target = name.ToLower();
           return await dbContext.Outcomes.FirstOrDefaultAsync(o => o.Name.ToLower() == target, cancellationToken);
        }
    }
}