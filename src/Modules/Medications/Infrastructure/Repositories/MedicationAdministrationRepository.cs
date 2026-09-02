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
    internal class MedicationAdministrationRepository(MedicationDbContext medicationDbContext) : IMedicalAdministrationRepository
    {
       
        public async Task AddAsync(MedicationAdministration administration)
        {
            await medicationDbContext.MedicationAdministrations.AddAsync(administration);
        }

        public async Task<MedicationAdministration?> Find(Guid orderId, DateTime scheduledFor)
        {
            return await medicationDbContext.MedicationAdministrations.FirstOrDefaultAsync
            ( x => x.MedicationOrderId == orderId && x.ScheduledFor == scheduledFor);
        }

        /// <summary>
        /// returns an enumerable of medicationadministered already within the entered time frame
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IEnumerable<MedicationAdministration> ForResidentBetween(Guid patientId, DateOnly from, DateOnly to)
        {
            var result = medicationDbContext.MedicationAdministrations
                        .Where(a => a.PatientId == patientId
                                && a.ScheduledFor >= from.ToDateTime(TimeOnly.MinValue)
                                && a.ScheduledFor <= to.ToDateTime(TimeOnly.MaxValue)).ToList();
            return result;
            
        }
    }
}