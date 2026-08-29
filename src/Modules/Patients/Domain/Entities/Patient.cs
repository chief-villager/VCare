using VCare.Modules.Patients.Domain.Events;
using VCare.SharedKernel.Domain;

namespace VCare.Modules.Patients.Domain.Entities;

public sealed class Patient : AggregateRoot
{
    // EF Core only.
    private Patient() { }

    private Patient(Guid id, string fullName, DateOnly dateOfBirth) : base(id)
    {
        FullName = fullName;
        DateOfBirth = dateOfBirth;
    }

    public string FullName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }

    public static Patient Register(string fullName, DateOnly dateOfBirth)
    {
        var patient = new Patient(Guid.NewGuid(), fullName, dateOfBirth);
        patient.Raise(new PatientRegistered(patient.Id));
        return patient;
    }

    public void Rename(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        FullName = fullName;
    }
}
