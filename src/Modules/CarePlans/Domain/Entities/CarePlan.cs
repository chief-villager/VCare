using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarePlans.Domain.Entities;
using VCare.SharedKernel.Domain;

namespace VCare.Modules.CarePlans.Domain.Entities
{
    public sealed class CarePlan : AggregateRoot
    {

        public Guid PatientId { get; private set; }
        public List<Diagnosis> Diagnoses { get; private set; } = [];
        public List<PatientGoals> Goals { get; private set; } = [];
        public List<Intervention> Intervention {get; private set;} = [];
        public DateOnly CreatedDate {get; private set;} 
        public DateOnly ModifiedDate {get; set;} 
        public string CreatedBy {get; private set;} = string.Empty;

        private CarePlan() { }
        private CarePlan(Guid patientId, Diagnosis diagnosis, List<PatientGoals> patientGoals, 
        List<Intervention> intervention, DateOnly createdDate, DateOnly modifiedDate, string createdBy)
        {
            Id = Guid.NewGuid();
            PatientId = patientId; 
            Goals = patientGoals;
            Intervention = intervention;
            CreatedDate = createdDate;
            ModifiedDate = modifiedDate;
            CreatedBy = createdBy;

        }

    }

}