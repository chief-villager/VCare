namespace VCare.Modules.Patients.Application.Dtos;

public sealed record PatientResponse(Guid Id, string FullName, DateOnly DateOfBirth);
