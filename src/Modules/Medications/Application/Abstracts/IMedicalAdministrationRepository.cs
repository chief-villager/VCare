using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;

namespace Medications.Application.Abstracts
{
    internal interface IMedicalAdministrationRepository
    {
        Task AddAsync(MedicationAdministration administration);
        Task<MedicationAdministration?> Find(Guid orderId, DateTime scheduledFor);
        IEnumerable<MedicationAdministration> ForResidentBetween(Guid residentId, DateOnly from, DateOnly to);
    }
}