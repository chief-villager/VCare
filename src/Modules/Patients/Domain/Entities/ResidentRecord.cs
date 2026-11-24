using VCare.SharedKernel.Auditing;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Domain.Entities;

/// <summary>Whether the resident still lives in the home.</summary>
public enum ResidencyStatus
{
    Current = 0,
    Discharged = 1,
    Deceased = 2
}

/// <summary>The two kinds of lasting power of attorney in England and Wales.</summary>
public enum AttorneyKind
{
    HealthAndWelfare = 0,
    PropertyAndFinancialAffairs = 1
}

/// <summary>A weight on a given day. Kept as a history: dosing and nutrition care need the trend.</summary>
internal sealed class PatientWeight : IAudited
{
    public Guid Id { get; private set; }
    public PatientId PatientId { get; private set; }
    public decimal Kilograms { get; private set; }
    public DateTime MeasuredAt { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public Guid RecordedByStaffId { get; private set; }

    private PatientWeight() { }

    internal static PatientWeight Create(PatientId patientId, decimal kilograms, DateTime measuredAt, Guid staffId, DateTime at) =>
        new()
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Kilograms = kilograms,
            MeasuredAt = measuredAt,
            RecordedAt = at,
            RecordedByStaffId = staffId
        };
}

/// <summary>
/// Someone with a registered lasting power of attorney for the resident. Not the
/// same as next of kin: only an attorney (for health and welfare) can consent or
/// refuse on the resident's behalf once they lack capacity. Never deleted: one
/// recorded in error is marked removed, with who removed it and why.
/// </summary>
internal sealed class PatientAttorney : IAudited
{
    public const int NameMaxLength = 200;
    public const int ContactMaxLength = 200;
    public const int ReasonMaxLength = 500;

    public Guid Id { get; private set; }
    public PatientId PatientId { get; private set; }
    public string Name { get; private set; } = null!;
    public AttorneyKind Kind { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public DateOnly? RegisteredOn { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public Guid RecordedByStaffId { get; private set; }
    public DateTime? RemovedAt { get; private set; }
    public Guid? RemovedByStaffId { get; private set; }
    public string? RemovalReason { get; private set; }
    public bool IsActive => RemovedAt is null;

    private PatientAttorney() { }

    internal static PatientAttorney Create(PatientId patientId, string name, AttorneyKind kind, string? phone, string? email,
        DateOnly? registeredOn, Guid staffId, DateTime at) =>
        new()
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Name = name,
            Kind = kind,
            Phone = phone,
            Email = email,
            RegisteredOn = registeredOn,
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
