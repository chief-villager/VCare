using VCare.SharedKernel.Domain;

namespace VCare.Modules.Visitation.Domain.Events;

public sealed record VisitScheduled(Guid VisitId, Guid PatientId) : IDomainEvent;

public sealed record VisitorCheckedIn(Guid VisitId, Guid PatientId, DateTime CheckedInAt) : IDomainEvent;

public sealed record VisitorCheckedOut(Guid VisitId, Guid PatientId, DateTime CheckedOutAt) : IDomainEvent;

public sealed record VisitCancelled(Guid VisitId, Guid PatientId, string Reason) : IDomainEvent;
