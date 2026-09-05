using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Medications.Application.Abstracts;
using Medications.Domain.Entities;
using Medications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;

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
            var medicationOrderId = new MedicationOrderId(orderId);
            return await medicationDbContext.MedicationAdministrations.FirstOrDefaultAsync
            ( x => x.MedicationOrderId == medicationOrderId && x.ScheduledFor == scheduledFor);
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
            var patient = new PatientId(patientId);
            var result = medicationDbContext.MedicationAdministrations
                        .Where(a => a.PatientId == patient
                                && a.ScheduledFor >= from.ToDateTime(TimeOnly.MinValue)
                                && a.ScheduledFor <= to.ToDateTime(TimeOnly.MaxValue)).ToList();
            return result;
            
        }


        
    }
}