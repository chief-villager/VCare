using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;

namespace Visitation.Domain.Entities
{
    internal class PersonalcareTask
    {
        public Guid VisitRecordId {get; private set;}
        public bool IsCareCompleted {get; private set;}
        public string CareTaskNote {get; private set;} = null!;

        private PersonalcareTask()
        {
            
        }

        public static PersonalcareTask  Create(Guid visitrecordId, bool isCareCompleted, string careTaskNote)
        {
            var newTask = new PersonalcareTask
            {
                VisitRecordId = visitrecordId,
                IsCareCompleted = isCareCompleted,
                CareTaskNote = careTaskNote,
            };
            return newTask; 
        }
    }
}