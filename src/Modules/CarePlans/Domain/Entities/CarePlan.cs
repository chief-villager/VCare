using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Domain.Entities;

// An aggregate root of its own. It references the patient it belongs to by id
// only: the Patients module owns the patient, this module owns the plan.
internal sealed class CarePlan : AggregateRoot<CarePlanId>
{
    public PatientId PatientId { get; private set; }
    public CareHomeId CareHomeId { get; private set; }
    public List<Diagnosis> Diagnoses { get; private set; } = [];
    public List<PatientGoals> Goals { get; private set; } = [];
    public List<Intervention> Intervention { get; private set; } = [];
    public DateOnly CreatedDate { get; private set; }
    public DateOnly ModifiedDate { get; private set; }
    public Guid StaffId { get; private set; }

    private CarePlan() { }

    public static Result<CarePlan> Create(
        Guid patientId,
        Guid staffId,
        Guid careHomeId,
        IEnumerable<string>? diagnoses = null,
        IEnumerable<string>? goals = null,
        IEnumerable<(string Description, bool Implemented)>? interventions = null)
    {
        if (patientId == Guid.Empty)
            return Result.Failure<CarePlan>("Patient is required.");
        if (careHomeId == Guid.Empty)
            return Result.Failure<CarePlan>("carehomeid is required");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var carePlan = new CarePlan
        {
            Id = CarePlanId.New(),
            PatientId = new PatientId(patientId),
            CareHomeId = new CareHomeId(careHomeId),
            StaffId = staffId,
            CreatedDate = today,
            ModifiedDate = today,
        };

        carePlan.ReplaceDetails(diagnoses, goals, interventions);
        return Result.Success(carePlan);
    }

    public Result Update(
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
