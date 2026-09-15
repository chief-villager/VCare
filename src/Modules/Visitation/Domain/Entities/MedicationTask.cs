using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using VCare.SharedKernel.Abstractions;

namespace Visitation.Domain.Entities
{
    internal class MedicationTask
    {
        public VisitId VisitId {get; private set;}
        public bool IsMedicationCompleted {get; private set;}
        public string MedicationTaskNote {get; private set;} = null!;

        private MedicationTask()
        {
            
        }

        public static MedicationTask  Create(Guid visitId, bool isMedicationCompleted, string medicationTaskNote)
        {
            var newTask = new MedicationTask
            {
                VisitId = new VisitId(visitId),
                IsMedicationCompleted = isMedicationCompleted,
                MedicationTaskNote = medicationTaskNote,
            };
            return newTask; 
        }
    }
}