using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Domain.Entities;

internal sealed class PatientGoals
{
    public CarePlanId CarePlanId { get; private set; }
    public string GoalDescription { get; set; } = string.Empty;

    public PatientGoals() { }

    public PatientGoals(CarePlanId carePlanId, string goalDescription)
    {
        GoalDescription = goalDescription;
        CarePlanId = carePlanId;
    }
}
