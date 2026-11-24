using VCare.SharedKernel.Auditing;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Domain.Entities;

/// <summary>
/// Whether anyone has asked. "No known allergies" is a recorded answer;
/// <see cref="Unknown"/> means nobody has recorded one, which is not the same
/// thing and must not be read as safe.
/// </summary>
public enum AllergyStatus
{
    Unknown = 0,
    NoKnownAllergies = 1,
    HasAllergies = 2
}

public enum AllergySeverity
{
    Unknown = 0,
    Mild = 1,
    Moderate = 2,
    Severe = 3
}

/// <summary>
/// A substance the resident reacts to. Never deleted: one entered in error is
/// marked removed, with who removed it and why, so the record shows what staff
/// were told at the time.
/// </summary>
internal sealed class PatientAllergy : IAudited
{
    public const int SubstanceMaxLength = 200;
    public const int TextMaxLength = 500;

    public Guid Id { get; private set; }
    public PatientId PatientId { get; private set; }
    public string Substance { get; private set; } = null!;
    public string? Reaction { get; private set; }
    public AllergySeverity Severity { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public Guid RecordedByStaffId { get; private set; }
    public DateTime? RemovedAt { get; private set; }
    public Guid? RemovedByStaffId { get; private set; }
    public string? RemovalReason { get; private set; }
    public bool IsActive => RemovedAt is null;

    private PatientAllergy() { }

    internal static PatientAllergy Create(PatientId patientId, string substance, string? reaction,
        AllergySeverity severity, Guid staffId, DateTime at) =>
        new()
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Substance = substance,
            Reaction = reaction,
            Severity = severity,
            RecordedAt = at,
            RecordedByStaffId = staffId
        };

    internal void Remove(string reason, Guid staffId, DateTime at)
    {
        RemovedAt = at;
        RemovedByStaffId = staffId;
        RemovalReason = reason;
    }
}

/// <summary>
/// A time the resident was away from the home: in hospital or on leave. Times
/// are wall-clock times in the home, like dose times, so Medications can tell
/// which doses fell while they were away.
/// </summary>
internal sealed class PatientAbsence : IAudited
{
    public const int NoteMaxLength = 500;

    public Guid Id { get; private set; }
    public PatientId PatientId { get; private set; }
    public AbsenceKind Kind { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public string? Note { get; private set; }
    public Guid RecordedByStaffId { get; private set; }
    public Guid? EndedByStaffId { get; private set; }
    public bool IsOpen => EndedAt is null;

    private PatientAbsence() { }

    internal static PatientAbsence Start(PatientId patientId, AbsenceKind kind, DateTime startedAt, string? note, Guid staffId) =>
        new()
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Kind = kind,
            StartedAt = startedAt,
            Note = note,
            RecordedByStaffId = staffId
        };

    internal void End(DateTime endedAt, Guid staffId)
    {
        EndedAt = endedAt;
        EndedByStaffId = staffId;
    }
}
