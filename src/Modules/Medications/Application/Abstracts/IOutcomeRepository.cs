using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;

namespace Medications.Application.Abstracts
{
    internal interface IOutcomeRepository
    {
        Task<OutcomeCode?>GetOutcomeByName(string name, CancellationToken cancellationToken);
    }
}