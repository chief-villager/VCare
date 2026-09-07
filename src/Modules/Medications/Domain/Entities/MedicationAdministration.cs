using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal class MedicationAdministration: Entity<MedicationAdministrationId>
    {
        
        public PatientId PatientId {get; private set;}
        public MedicationOrderId MedicationOrderId { get; private set; }

        public DateTime? ScheduledFor { get; private set; }   // null for PRN
        public DateTime? AdministeredAt { get; private set; }
        public Guid OutcomeCodeId { get; private set; }       // Given, Refused, Omitted...
        public Guid AdministeredByStaffId { get; private set; }
        public string? Notes { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ModifiedAt { get; private set; }

        private MedicationAdministration(){}


        public static Result<MedicationAdministration> Create(Guid medicationOrderId, Guid patientId, DateTime? scheduledFor, 
        DateTime? admninisteredAt, Guid outcomeId, Guid administeredByStaffId, 
         string? notes, DateTime? modifiedAt = null)
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
            if (patientId == Guid.Empty )
            {
                return Result.Failure<MedicationAdministration>("patientId id required");
            };
    
            var administration = new MedicationAdministration
            {
                Id = MedicationAdministrationId.New(),
                MedicationOrderId = new MedicationOrderId(medicationOrderId),
                PatientId = new PatientId(patientId),
                ScheduledFor = scheduledFor,
                AdministeredAt = admninisteredAt,
                OutcomeCodeId = outcomeId,
                AdministeredByStaffId = administeredByStaffId,
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