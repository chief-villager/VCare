using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Domain.Entities;

internal sealed class Intervention
{
    public CarePlanId CarePlanId { get; private set; }
    public string Description { get; set; } = string.Empty;
    public bool Implementation { get; set; }

    public Intervention() { }

    public Intervention(CarePlanId carePlanId, string description, bool implementation)
    {
        Description = description;
        Implementation = implementation;
        CarePlanId = carePlanId;
    }
}
