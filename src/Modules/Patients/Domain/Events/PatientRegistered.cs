using VCare.SharedKernel.Domain;

namespace VCare.Modules.Patients.Domain.Events;

public sealed record PatientRegistered(Guid PatientId) : IDomainEvent;
