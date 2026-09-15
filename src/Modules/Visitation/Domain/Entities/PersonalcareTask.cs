using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;

namespace Visitation.Domain.Entities
{
    internal class PersonalcareTask
    {
        public VisitId VisitId {get; private set;}
        public bool IsCareCompleted {get; private set;}
        public string CareTaskNote {get; private set;} = null!;

        private PersonalcareTask()
        {
            
        }

        public static PersonalcareTask  Create(Guid visitId, bool isCareCompleted, string careTaskNote)
        {
            var newTask = new PersonalcareTask
            {
                VisitId = new VisitId(visitId),
                IsCareCompleted = isCareCompleted,
                CareTaskNote = careTaskNote,
            };
            return newTask; 
        }
    }
}