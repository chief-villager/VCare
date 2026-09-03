using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal class MedicationAdministration
    {
        public Guid Id { get; private set; }
        
        public Guid PatientId {get; private set;}
        public Guid MedicationOrderId { get; private set; }

        public DateTime? ScheduledFor { get; private set; }   // null for PRN
        public DateTime? AdministeredAt { get; private set; }
        public Guid OutcomeCodeId { get; private set; }       // Given, Refused, Omitted...
        public Guid AdministeredByStaffId { get; private set; }
        public Guid? WitnessedByStaffId { get; private set; } // controlled drugs
        public string? Notes { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ModifiedAt { get; private set; }

        private MedicationAdministration(){}


        public static Result<MedicationAdministration> Create(Guid medicationOrderId, DateTime? scheduledFor, 
        DateTime? admninisteredAt, Guid outcomeId, Guid administeredByStaffId, 
        Guid? witnessedByStaffId, string? notes, DateTime? modifiedAt = null)
        {
            if (medicationOrderId == Guid.Empty)
            {
                return Result.Failure<MedicationAdministration>("medicationOrderId id required");
            }
            if ( outcomeId == Guid.Empty)
            {
                return Result.Failure<MedicationAdministration>("outcomeId id required");
            }
            if ( administeredByStaffId == Guid.Empty)
            {
                return Result.Failure<MedicationAdministration>("StaffId id required");
            }
            var administration = new MedicationAdministration
            {
                Id = Guid.NewGuid(),
                MedicationOrderId = medicationOrderId,
                ScheduledFor = scheduledFor,
                AdministeredAt = admninisteredAt,
                OutcomeCodeId = outcomeId,
                AdministeredByStaffId = administeredByStaffId,
                WitnessedByStaffId = witnessedByStaffId,
                Notes = notes,
                CreatedAt = DateTime.Now
            };
            if (modifiedAt.HasValue)
            {
                administration.ModifiedAt = modifiedAt.Value;
            }
            return Result.Success(administration);
            
        }


       
    }
}