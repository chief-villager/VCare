using VCare.Modules.Patients.Domain.Events;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Domain.Entities;

internal sealed class Patient : AggregateRoot<PatientId>
{
    public string Firstname { get; private set; } = null!;
    public string Lastname { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public string Gender { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    // EF Core only.
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public DateTime? ModifiedAt { get; private set; } = null;
    public string? PhoneNumber { get; private set; } = null!;
    public string? Email { get; private set; } = null!;
    public string EmergencyContactName { get; private set; } = null!;
    public string EmergencyContactPhoneNumber { get; private set; } = null!;
    public string EmergencyContactRelationship { get; private set; } = null!;
    public string FullName => $"{Firstname} {Lastname}";
    private Patient() { }


    

    public static Result<Patient> Register(string firstname, string lastname, 
    string gender,string address, DateOnly dateOfBirth, 
    string emergencyContactRelationship, string emergencyContactPhoneNumber,
    string emergencyContactName, string? phoneNumber, string? email, DateTime? modifiedAt = null)
    {
        if (string.IsNullOrWhiteSpace(firstname))
            return Result.Failure<Patient>("Firstname is required.");
        if (string.IsNullOrWhiteSpace(lastname))
            return Result.Failure<Patient>("Lastname is required.");
        if (string.IsNullOrWhiteSpace(gender))
            return Result.Failure<Patient>("Gender is required.");
        if (string.IsNullOrWhiteSpace(address))
            return Result.Failure<Patient>("Address is required.");
        if (string.IsNullOrWhiteSpace(emergencyContactName))
            return Result.Failure<Patient>("Emergency contact name is required.");
        if (string.IsNullOrWhiteSpace(emergencyContactPhoneNumber))
            return Result.Failure<Patient>("Emergency contact phone number is required.");
        if (string.IsNullOrWhiteSpace(emergencyContactRelationship))
            return Result.Failure<Patient>("Emergency contact relationship is required.");
        var patient = new Patient
        {
            Id = PatientId.New(),
            Firstname = firstname,
            Lastname = lastname,
            Gender = gender,
            Address = address,
            DateOfBirth = dateOfBirth,
            ModifiedAt = modifiedAt,
            EmergencyContactPhoneNumber = emergencyContactPhoneNumber,
            EmergencyContactRelationship = emergencyContactRelationship,
            EmergencyContactName = emergencyContactName,
            PhoneNumber = phoneNumber,
            Email = email
        };
        patient.Raise(new PatientRegistered(patient.Id.Value));
        return Result.Success(patient);
    }

    public void Update(string? firstname = null, string? lastname = null, 
    string? gender = null,string? address = null, DateOnly? dateOfBirth = null, 
    string? phoneNumber = null, string? email = null, string? emergencyContactName = null,
    string? emergencyContactPhoneNumber = null, string? emergencyContactRelationship = null)
    {
        if (!string.IsNullOrWhiteSpace(firstname))
            Firstname = firstname;
        if (!string.IsNullOrWhiteSpace(lastname))
            Lastname = lastname;
        if (!string.IsNullOrWhiteSpace(gender))
            Gender = gender;
        if (!string.IsNullOrWhiteSpace(address))
            Address = address;
        if (dateOfBirth.HasValue)
            DateOfBirth = dateOfBirth.Value;
        ModifiedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(phoneNumber))
            PhoneNumber = phoneNumber;
        if (!string.IsNullOrWhiteSpace(email))
            Email = email;
        if (!string.IsNullOrWhiteSpace(emergencyContactName))
            EmergencyContactName = emergencyContactName;
        if (!string.IsNullOrWhiteSpace(emergencyContactPhoneNumber))
            EmergencyContactPhoneNumber = emergencyContactPhoneNumber;
        if (!string.IsNullOrWhiteSpace(emergencyContactRelationship))
            EmergencyContactRelationship = emergencyContactRelationship;
        ModifiedAt = DateTime.Now;
    }
}
