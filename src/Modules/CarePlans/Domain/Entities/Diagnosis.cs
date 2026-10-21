using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Domain.Entities;

internal sealed class Diagnosis
{
    public CarePlanId CarePlanId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? GoalDescription { get; set; }
    public string? Intervention { get; set; } 

    private Diagnosis() { }

    private Diagnosis(CarePlanId carePlanId, string name, string? goalDescription = null, string? intervention = null)
    {
        Name = name;
        GoalDescription = goalDescription;
        Intervention = intervention;
        CarePlanId = carePlanId;
    }

    public static Diagnosis Create(CarePlanId carePlanId, string name, string? goalDescription = null, string? intervention = null) => new(carePlanId, name, goalDescription, intervention);
}
