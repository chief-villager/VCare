using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Domain.Entities;

namespace VCare.Modules.Patients.Application.Services;

public  class PatientService(IPatientRepository repository)
{
    public async Task<PatientResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        return patient is null
            ? null
            : new PatientResponse(patient.Id, patient.FullName, patient.DateOfBirth);
    }

    public async Task<Guid> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = Patient.Register(request.FullName, request.DateOfBirth);
        await repository.AddAsync(patient, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return patient.Id;
    }
}
