using System.Text.Json;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.CarePlans.Domain.Entities;

/// <summary>
/// The care plan as it stood after one edit: its goal and diagnoses, who made
/// the edit and when. Never changed, so an earlier version can always be read back.
/// </summary>
internal sealed class CarePlanVersion
{
    public Guid Id { get; private set; }
    public CarePlanId CarePlanId { get; private set; }
    public int Number { get; private set; }
    public string Goal { get; private set; } = null!;

    /// <summary>JSON: the diagnoses, each with its goal and intervention.</summary>
    public string Diagnoses { get; private set; } = null!;
    public Guid EditedByStaffId { get; private set; }

    /// <summary>UTC.</summary>
    public DateTime EditedAt { get; private set; }

    private CarePlanVersion() { }

    internal static CarePlanVersion Of(CarePlan plan, Guid staffId, DateTime at) => new()
    {
        Id = Guid.NewGuid(),
        CarePlanId = plan.Id,
        Number = plan.Version,
        Goal = plan.Goal,
        Diagnoses = JsonSerializer.Serialize(plan.Diagnoses.Select(d => new DiagnosisSnapshot(d.Name, d.GoalDescription, d.Intervention))),
        EditedByStaffId = staffId,
        EditedAt = at
    };

    internal IReadOnlyList<DiagnosisSnapshot> ReadDiagnoses() =>
        JsonSerializer.Deserialize<List<DiagnosisSnapshot>>(Diagnoses) ?? [];
}

internal sealed record DiagnosisSnapshot(string Name, string? GoalDescription, string? Intervention);
