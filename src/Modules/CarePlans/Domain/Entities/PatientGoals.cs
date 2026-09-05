using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;

namespace CarePlans.Domain.Entities
{
    public class PatientGoals
    {
        public CarePlanId CarePlanId {get; private set;}

        public  string GoalDescription { get;  set; } = string.Empty;
        public PatientGoals(){}

        public PatientGoals(CarePlanId carePlanId, string goalDescription)
        {
            GoalDescription = goalDescription;
            CarePlanId = carePlanId;
        }
        
    }
}