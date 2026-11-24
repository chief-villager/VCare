using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Patients.Application.Dtos;

/// <summary>Severity is Unknown, Mild, Moderate or Severe.</summary>
public sealed record RecordAllergyRequest(string Substance, string? Reaction, string? Severity);

public sealed record RemoveAllergyRequest(string Reason);

public sealed record AllergyResponse(Guid Id, string Substance, string? Reaction, AllergySeverity Severity,
    DateTime RecordedAt, Guid RecordedByStaffId, DateTime? RemovedAt, string? RemovalReason);

public sealed record AllergiesResponse(AllergyStatus Status, IReadOnlyList<AllergyResponse> Allergies);

/// <summary>
/// Kind is Hospital or Leave. StartedAt is the time in the home; it defaults to
/// now, for a resident leaving as it is recorded.
/// </summary>
public sealed record StartAbsenceRequest(string Kind, DateTime? StartedAt, string? Note);

public sealed record EndAbsenceRequest(DateTime? EndedAt);

public sealed record AbsenceResponse(Guid Id, AbsenceKind Kind, DateTime StartedAt, DateTime? EndedAt, string? Note,
    Guid RecordedByStaffId, Guid? EndedByStaffId);

/// <summary>
/// The details that come with admission. Every field is set by each call: one
/// left empty clears that detail. The NHS number may be written with spaces.
/// </summary>
public sealed record UpdateCareDetailsRequest(string? NhsNumber, string? GpName, string? GpPractice, string? GpPhone,
    string? Room, DateOnly? AdmittedOn);

/// <summary>At is the time in the home; it defaults to now.</summary>
public sealed record DischargeRequest(DateTime? At, string? Destination);

/// <summary>At is the time in the home; it defaults to now.</summary>
public sealed record RecordDeathRequest(DateTime? At);

/// <summary>At is when the resident came back, as the time in the home; it defaults to now.</summary>
public sealed record ReadmitRequest(DateTime? At);

/// <summary>MeasuredAt is the time in the home; it defaults to now. Stored to one decimal place.</summary>
public sealed record RecordWeightRequest(decimal Kilograms, DateTime? MeasuredAt);

public sealed record WeightResponse(Guid Id, decimal Kilograms, DateTime MeasuredAt, Guid RecordedByStaffId);

/// <summary>Kind is HealthAndWelfare or PropertyAndFinancialAffairs.</summary>
public sealed record RecordAttorneyRequest(string Name, string Kind, string? Phone, string? Email, DateOnly? RegisteredOn);

public sealed record RemoveAttorneyRequest(string Reason);

public sealed record AttorneyResponse(Guid Id, string Name, AttorneyKind Kind, string? Phone, string? Email,
    DateOnly? RegisteredOn, DateTime RecordedAt, Guid RecordedByStaffId, DateTime? RemovedAt, string? RemovalReason);
