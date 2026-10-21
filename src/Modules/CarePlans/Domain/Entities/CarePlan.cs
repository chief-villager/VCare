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
    public DateOnly CreatedDate { get; private set; }
    public DateOnly ModifiedDate { get; private set; }
    public string Goal {get; private set;} = default!;
    public Guid StaffId { get; private set; }

    private CarePlan() { }

    public static Result<CarePlan> Create(
        Guid patientId,
        string goal,
        Guid staffId,
        Guid careHomeId,
        IEnumerable<(string Name, string? GoalDescription, string? Intervention)>? diagnoses = null
       )
    {
        if (patientId == Guid.Empty)
            return Result.Failure<CarePlan>("Patient is required.");
        if (string.IsNullOrWhiteSpace(goal))
        {
            return Result.Failure<CarePlan>(new Error("400","Goal is required"));
        }
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

        carePlan.ReplaceDetails(diagnoses,goal);
        return Result.Success(carePlan);
    }

    public Result Update(
        IEnumerable<(string Name, string? GoalDescription, string? Intervention)>? diagnoses = null, string? goal = null)
    {
        ReplaceDetails(diagnoses,goal);
        ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        return Result.Success();
    }

    private void ReplaceDetails(
        IEnumerable<(string Name, string? GoalDescription, string? Intervention)>? diagnoses, string? goal)
    {
        if (goal is not null)
        {
            Goal = goal;
        }
        if (diagnoses is not null)
            Diagnoses = [.. diagnoses.Select(d => Diagnosis.Create(Id, d.Name, d.GoalDescription, d.Intervention))];

    }
}
