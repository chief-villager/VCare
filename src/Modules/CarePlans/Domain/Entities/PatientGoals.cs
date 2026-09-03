using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarePlans.Domain.Entities
{
    public class PatientGoals
    {
        public Guid CarePlanId {get; private set;}

        public  string GoalDescription { get;  set; } = string.Empty;
        public PatientGoals(){}

        public PatientGoals(Guid carePlanId, string goalDescription)
        {
            GoalDescription = goalDescription;
            CarePlanId = carePlanId;
        }
        
    }
}