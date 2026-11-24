using VCare.SharedKernel.Auditing;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Domain.Entities;

// An aggregate root of its own. It references the patient it belongs to by id
// only: the Patients module owns the patient, this module owns the plan.
internal sealed class CarePlan : AggregateRoot<CarePlanId>, IAudited
{
    public PatientId PatientId { get; private set; }
    public CareHomeId CareHomeId { get; private set; }
    public List<Diagnosis> Diagnoses { get; private set; } = [];
    public DateOnly CreatedDate { get; private set; }
    public DateOnly ModifiedDate { get; private set; }
    public string Goal {get; private set;} = default!;
    public Guid StaffId { get; private set; }

    /// <summary>Goes up by one with every edit. An edit names the version it was made to.</summary>
    public int Version { get; private set; }
    public List<CarePlanVersion> Versions { get; private set; } = [];

    private CarePlan() { }

    internal static Error EditConflict(int? current) => Error.Conflict("CarePlans.EditConflict",
        "Someone else changed this care plan after you opened it"
        + (current is { } v ? $" (it is now version {v})" : "")
        + ". Reload it and make your change again.");

    public static Result<CarePlan> Create(
        Guid patientId,
        string goal,
        Guid staffId,
        Guid careHomeId,
        IEnumerable<(string Name, string? GoalDescription, string? Intervention)>? diagnoses = null,
        DateTime? at = null
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
        carePlan.Version = 1;
        carePlan.Versions.Add(CarePlanVersion.Of(carePlan, staffId, at ?? DateTime.UtcNow));
        return Result.Success(carePlan);
    }

    /// <summary>
    /// Applies an edit made to <paramref name="basedOnVersion"/>. If the plan has
    /// moved on since, the edit is refused: applying it would silently undo
    /// whatever the other person changed.
    /// </summary>
    public Result Update(
        IEnumerable<(string Name, string? GoalDescription, string? Intervention)>? diagnoses, string? goal,
        int basedOnVersion, Guid staffId, DateTime at)
    {
        if (basedOnVersion != Version)
            return Result.Failure(EditConflict(Version));
        ReplaceDetails(diagnoses,goal);
        ModifiedDate = DateOnly.FromDateTime(at);
        Version++;
        Versions.Add(CarePlanVersion.Of(this, staffId, at));
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
