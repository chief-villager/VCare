using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Domain.Entities;

internal sealed class Diagnosis
{
    public CarePlanId CarePlanId { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private Diagnosis() { }

    private Diagnosis(CarePlanId carePlanId, string description)
    {
        Description = description;
        CarePlanId = carePlanId;
    }

    public static Diagnosis Create(CarePlanId carePlanId, string description) => new(carePlanId, description);
}
