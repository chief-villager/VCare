using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;

namespace Visitation.Domain.Entities
{
    internal class FeedingTask
    {
        public Guid VisitRecordId {get; private set;}

        public bool IsFeedingCompleted {get; private set;}
        public string FeedingTaskNote {get; private set;} = null!;

        private FeedingTask()
        {
            
        }

        public static FeedingTask  Create(Guid visitRecordId, bool isFeedingCompleted, string feedingTaskNote)
        {
            var newTask = new FeedingTask
            {
                VisitRecordId = visitRecordId,
                IsFeedingCompleted = isFeedingCompleted,
                FeedingTaskNote = feedingTaskNote,
            };
            return newTask; 
        }
    }
}