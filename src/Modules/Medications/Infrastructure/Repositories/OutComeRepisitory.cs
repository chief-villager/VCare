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
           return await dbContext.Outcomes.FirstOrDefaultAsync(o => o.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase), cancellationToken);
        }
    }
}