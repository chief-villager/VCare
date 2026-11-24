using VCare.Modules.Patients.Domain.Entities;

namespace VCare.Modules.Patients.Application.Dtos;

// AllergyStatus is on the summary so whoever is looking at the resident sees
// it without asking: Unknown is not the same as no known allergies. Residency
// says whether they still live here; LeftAt is when they were discharged or died.
public sealed record PatientResponse(Guid Id, string FullName, DateOnly DateOfBirth,
    AllergyStatus AllergyStatus = default,
    string? NhsNumber = null, string? Room = null,
    string? GpName = null, string? GpPractice = null, string? GpPhone = null,
    DateOnly? AdmittedOn = null, ResidencyStatus Residency = default, DateTime? LeftAt = null,
    decimal? LatestWeightKg = null, DateTime? LatestWeightMeasuredAt = null)
{
    internal static PatientResponse From(Patient p) => new(p.Id.Value, p.FullName, p.DateOfBirth, p.AllergyStatus,
        p.NhsNumber, p.Room, p.GpName, p.GpPractice, p.GpPhone, p.AdmittedOn, p.Residency, p.LeftAt,
        p.LatestWeight?.Kilograms, p.LatestWeight?.MeasuredAt);
}

/// <summary>
/// A row of the resident list: enough to find the right resident and see whether
/// they have allergies. Their NHS number, GP and weight are on <see cref="PatientResponse"/>,
/// read one resident at a time so each read goes in that resident's audit trail.
/// </summary>
public sealed record PatientListItem(Guid Id, string FullName, DateOnly DateOfBirth, AllergyStatus AllergyStatus,
    string? Room, ResidencyStatus Residency)
{
    internal static PatientListItem From(Patient p) =>
        new(p.Id.Value, p.FullName, p.DateOfBirth, p.AllergyStatus, p.Room, p.Residency);
}
