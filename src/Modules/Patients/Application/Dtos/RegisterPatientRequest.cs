namespace VCare.Modules.Patients.Application.Dtos;

public sealed record RegisterPatientRequest(string FullName, DateOnly DateOfBirth);
