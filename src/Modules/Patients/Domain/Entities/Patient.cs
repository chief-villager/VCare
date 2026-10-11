using Patients.Domain.Entities;
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
    public CareHomeId CareHomeId {get; private set;}
    public DateTime? ModifiedAt { get; private set; } = null;
    public string? PhoneNumber { get; private set; } = null!;
    public string? Email { get; private set; } = null!;
    public string EmergencyContactName { get; private set; } = null!;
    public string EmergencyContactPhoneNumber { get; private set; } = null!;
    public string EmergencyContactRelationship { get; private set; } = null!;
    public string? ProfilePictureUrl {get; private set;}
    public List<PatientDocument> Documents {get; private set;} = new List<PatientDocument>();
    public IReadOnlyList<PatientDocument> GetDocuments() => Documents.AsReadOnly();
   
    public string FullName => $"{Firstname} {Lastname}";
    private Patient() { }


    public Result UpdateProfilePicture(string profilePictureUrl)
    {
        if (string.IsNullOrWhiteSpace(profilePictureUrl))
            return Result.Failure(new Error("400", "Profile picture URL cannot be empty."));
        ProfilePictureUrl = profilePictureUrl;
        ModifiedAt = DateTime.Now;
        return Result.Success();
    }

    public static Result<Patient> Register(string firstname, string lastname, 
    string gender,string address, DateOnly dateOfBirth, 
    string emergencyContactRelationship, string emergencyContactPhoneNumber,
    string emergencyContactName, string? phoneNumber, Guid careHomeId, string? email, DateTime? CreatedAt = null)
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
        if (careHomeId == Guid.Empty)
        {
            return Result.Failure<Patient>("carehomeid is required");

        }
        var patient = new Patient
        {
            Id = PatientId.New(),
            Firstname = firstname,
            Lastname = lastname,
            Gender = gender,
            Address = address,
            DateOfBirth = dateOfBirth,
            CreatedAt = CreatedAt ?? DateTime.Now,
            EmergencyContactPhoneNumber = emergencyContactPhoneNumber,
            EmergencyContactRelationship = emergencyContactRelationship,
            EmergencyContactName = emergencyContactName,
            PhoneNumber = phoneNumber,
            CareHomeId = new CareHomeId(careHomeId),
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

    public  Result<PatientDocument> AddDocument(string fileName, string storageKeyName)
    {
        var documentResult = PatientDocument.Create(fileName, storageKeyName, Id);
        if (documentResult.IsFailure)
        {
            return Result.Failure<PatientDocument>(documentResult.Error);
        }
        Documents.Add(documentResult.Value);
        ModifiedAt = DateTime.Now;
        return Result.Success(documentResult.Value);
    }

}
