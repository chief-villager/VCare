using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Domain.Entities;

// Child entity within the Patient aggregate. It is created and mutated only
// through the Patient aggregate root, never persisted on its own.
internal sealed class CarePlan : Entity<CarePlanId>
{
    public PatientId PatientId { get; private set; }
    public List<Diagnosis> Diagnoses { get; private set; } = [];
    public List<PatientGoals> Goals { get; private set; } = [];
    public List<Intervention> Intervention { get; private set; } = [];
    public DateOnly CreatedDate { get; private set; }
    public DateOnly ModifiedDate { get; private set; }
    public Guid StaffId { get; private set; }

    private CarePlan() { }

    internal static Result<CarePlan> Create(
        PatientId patientId,
        Guid staffId,
        IEnumerable<string>? diagnoses = null,
        IEnumerable<string>? goals = null,
        IEnumerable<(string Description, bool Implemented)>? interventions = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var carePlan = new CarePlan
        {
            Id = CarePlanId.New(),
            PatientId = patientId,
            StaffId = staffId,
            CreatedDate = today,
            ModifiedDate = today,
            
        };

        carePlan.ReplaceDetails(diagnoses, goals, interventions);
        return Result.Success(carePlan);
    }

    internal Result Update(
        IEnumerable<string>? diagnoses = null,
        IEnumerable<string>? goals = null,
        IEnumerable<(string Description, bool Implemented)>? interventions = null)
    {
        ReplaceDetails(diagnoses, goals, interventions);
        ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        return Result.Success();
    }

    private void ReplaceDetails(
        IEnumerable<string>? diagnoses,
        IEnumerable<string>? goals,
        IEnumerable<(string Description, bool Implemented)>? interventions)
    {
        if (diagnoses is not null)
            Diagnoses = [.. diagnoses.Select(d => Diagnosis.Create(Id, d))];

        if (goals is not null)
            Goals = [.. goals.Select(g => new PatientGoals(Id, g))];

        if (interventions is not null)
            Intervention = [.. interventions.Select(i => new Intervention(Id, i.Description, i.Implemented))];
    }
}
