using VCare.SharedKernel.Auditing;
using Patients.Domain.Entities;
using VCare.Modules.Patients.Domain.Events;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Domain.Entities;

internal sealed class Patient : AggregateRoot<PatientId>, IAudited
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

    public AllergyStatus AllergyStatus { get; private set; }
    public List<PatientAllergy> Allergies { get; private set; } = [];
    public List<PatientAbsence> Absences { get; private set; } = [];

    public string? NhsNumber { get; private set; }
    public string? GpName { get; private set; }
    public string? GpPractice { get; private set; }
    public string? GpPhone { get; private set; }
    public string? Room { get; private set; }
    public DateOnly? AdmittedOn { get; private set; }
    public ResidencyStatus Residency { get; private set; }
    public DateTime? LeftAt { get; private set; }
    public string? DischargeDestination { get; private set; }
    public List<PatientWeight> Weights { get; private set; } = [];
    public List<PatientAttorney> Attorneys { get; private set; } = [];
    public PatientWeight? LatestWeight => Weights.MaxBy(w => w.MeasuredAt);

    private const int DetailMaxLength = 200;
    private const int PhoneMaxLength = 30;
    private const int RoomMaxLength = 50;

    /// <summary>
    /// The details that come with admission. Each call sets all of them: a value
    /// left empty clears that detail.
    /// </summary>
    public Result UpdateCareDetails(string? nhsNumber, string? gpName, string? gpPractice, string? gpPhone,
        string? room, DateOnly? admittedOn, DateTime at)
    {
        string? nhs = null;
        if (!string.IsNullOrWhiteSpace(nhsNumber))
        {
            nhs = NhsNumbers.Normalise(nhsNumber);
            if (nhs is null)
                return Result.Failure(Error.Validation("Patients.InvalidNhsNumber",
                    "That isn't a valid NHS number: it must be 10 digits, and the last is a check digit."));
        }
        foreach (var (value, name, max) in new[]
                 {
                     (gpName, "GP name", DetailMaxLength), (gpPractice, "GP practice", DetailMaxLength),
                     (gpPhone, "GP phone number", PhoneMaxLength), (room, "Room", RoomMaxLength)
                 })
        {
            if (value?.Trim().Length > max)
                return Result.Failure(Error.Validation("Patients.DetailTooLong", $"{name} can be at most {max} characters."));
        }

        NhsNumber = nhs;
        GpName = Clean(gpName);
        GpPractice = Clean(gpPractice);
        GpPhone = Clean(gpPhone);
        Room = Clean(room);
        AdmittedOn = admittedOn;
        ModifiedAt = at;
        return Result.Success();
    }

    public Result<PatientWeight> RecordWeight(decimal kilograms, DateTime measuredAt, Guid staffId, DateTime at)
    {
        if (kilograms <= 0 || kilograms > 400)
            return Result.Failure<PatientWeight>(Error.Validation("Patients.InvalidWeight",
                "The weight must be more than 0 and at most 400 kg."));
        var weight = PatientWeight.Create(Id, decimal.Round(kilograms, 1), measuredAt, staffId, at);
        Weights.Add(weight);
        ModifiedAt = at;
        return Result.Success(weight);
    }

    /// <summary>The resident leaves the home for good. Medications expects no dose after this.</summary>
    public Result Discharge(DateTime at, string? destination, Guid staffId)
    {
        if (destination?.Trim().Length > DetailMaxLength)
            return Result.Failure(Error.Validation("Patients.DetailTooLong", $"The destination can be at most {DetailMaxLength} characters."));
        var left = Leave(ResidencyStatus.Discharged, at, staffId);
        if (left.IsFailure)
            return left;
        DischargeDestination = Clean(destination);
        return Result.Success();
    }

    /// <summary>The resident died. Medications expects no dose after this.</summary>
    public Result RecordDeath(DateTime at, Guid staffId) => Leave(ResidencyStatus.Deceased, at, staffId);

    /// <summary>
    /// A discharged resident comes back, or a discharge is undone. <paramref name="returnedAt"/>
    /// is when they came back, which can be before it is entered: doses from then on
    /// are the home's again. It is also their new admission date.
    /// </summary>
    public Result Readmit(DateTime returnedAt, Guid staffId, DateTime at)
    {
        if (Residency != ResidencyStatus.Discharged)
            return Result.Failure(Error.Conflict("Patients.NotDischarged", "Only a discharged resident can be readmitted."));
        if (returnedAt < LeftAt)
            return Result.Failure(Error.Validation("Patients.ReturnBeforeLeaving", "The return cannot be before the discharge."));
        // The time away stays on the record as a closed absence, so doses due while
        // they were discharged are still not counted as missed.
        var away = PatientAbsence.Start(Id, AbsenceKind.Discharged, LeftAt!.Value, DischargeDestination, staffId);
        away.End(returnedAt, staffId);
        Absences.Add(away);
        Residency = ResidencyStatus.Current;
        LeftAt = null;
        DischargeDestination = null;
        AdmittedOn = DateOnly.FromDateTime(returnedAt);
        ModifiedAt = at;
        return Result.Success();
    }

    private static readonly Error NotResident = Error.Conflict("Patients.NotResident", "This resident has already left the home.");

    private Result Leave(ResidencyStatus status, DateTime at, Guid staffId)
    {
        if (Residency != ResidencyStatus.Current)
            return Result.Failure(NotResident);
        if (Absences.FirstOrDefault(a => a.IsOpen) is { } away)
        {
            if (at < away.StartedAt)
                return Result.Failure(Error.Validation("Patients.LeftBeforeAbsence",
                    "That is before the resident's current absence began."));
            // A hospital stay ends where the residency does.
            away.End(at, staffId);
        }
        Residency = status;
        LeftAt = at;
        ModifiedAt = at;
        return Result.Success();
    }

    public Result<PatientAttorney> RecordAttorney(string name, AttorneyKind kind, string? phone, string? email,
        DateOnly? registeredOn, Guid staffId, DateTime at)
    {
        var who = name?.Trim();
        if (string.IsNullOrEmpty(who))
            return Result.Failure<PatientAttorney>(Error.Validation("Patients.AttorneyNameRequired", "The attorney's name is required."));
        if (who.Length > PatientAttorney.NameMaxLength)
            return Result.Failure<PatientAttorney>(Error.Validation("Patients.DetailTooLong",
                $"The name can be at most {PatientAttorney.NameMaxLength} characters."));
        if (phone?.Trim().Length > PhoneMaxLength || email?.Trim().Length > PatientAttorney.ContactMaxLength)
            return Result.Failure<PatientAttorney>(Error.Validation("Patients.DetailTooLong", "A contact detail is too long."));

        var attorney = PatientAttorney.Create(Id, who, kind, Clean(phone), Clean(email), registeredOn, staffId, at);
        Attorneys.Add(attorney);
        ModifiedAt = at;
        return Result.Success(attorney);
    }

    /// <summary>Marks an attorney recorded in error, or no longer acting, as removed. It stays on the record.</summary>
    public Result RemoveAttorney(Guid attorneyId, string? reason, Guid staffId, DateTime at)
    {
        var attorney = Attorneys.FirstOrDefault(a => a.Id == attorneyId && a.IsActive);
        if (attorney is null)
            return Result.Failure(Error.NotFound("Patients.AttorneyNotFound", "Attorney not found."));
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("Patients.RemovalReasonRequired", "Say why the attorney is being removed."));
        if (reason.Trim().Length > PatientAttorney.ReasonMaxLength)
            return Result.Failure(Error.Validation("Patients.DetailTooLong",
                $"The reason can be at most {PatientAttorney.ReasonMaxLength} characters."));
        attorney.Remove(reason.Trim(), staffId, at);
        ModifiedAt = at;
        return Result.Success();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Patient() { }

    public Result<PatientAllergy> RecordAllergy(string substance, string? reaction, AllergySeverity severity, Guid staffId, DateTime at)
    {
        var name = substance?.Trim();
        if (string.IsNullOrEmpty(name))
            return Result.Failure<PatientAllergy>("The substance is required.");
        if (name.Length > PatientAllergy.SubstanceMaxLength)
            return Result.Failure<PatientAllergy>($"The substance can be at most {PatientAllergy.SubstanceMaxLength} characters.");
        if (reaction?.Trim().Length > PatientAllergy.TextMaxLength)
            return Result.Failure<PatientAllergy>($"The reaction can be at most {PatientAllergy.TextMaxLength} characters.");
        if (staffId == Guid.Empty)
            return Result.Failure<PatientAllergy>("StaffId id required");
        if (Allergies.Any(a => a.IsActive && string.Equals(a.Substance, name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<PatientAllergy>(Error.Conflict("Patients.AllergyAlreadyRecorded",
                $"An allergy to {name} is already recorded."));

        var allergy = PatientAllergy.Create(Id, name, string.IsNullOrWhiteSpace(reaction) ? null : reaction.Trim(), severity, staffId, at);
        Allergies.Add(allergy);
        AllergyStatus = AllergyStatus.HasAllergies;
        ModifiedAt = at;
        return Result.Success(allergy);
    }

    /// <summary>Marks an allergy entered in error as removed. It stays on the record.</summary>
    public Result RemoveAllergy(Guid allergyId, string? reason, Guid staffId, DateTime at)
    {
        var allergy = Allergies.FirstOrDefault(a => a.Id == allergyId && a.IsActive);
        if (allergy is null)
            return Result.Failure(Error.NotFound("Patients.AllergyNotFound", "Allergy not found."));
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(Error.Validation("Patients.RemovalReasonRequired", "Say why the allergy is being removed."));
        if (reason.Trim().Length > PatientAllergy.TextMaxLength)
            return Result.Failure($"The reason can be at most {PatientAllergy.TextMaxLength} characters.");

        allergy.Remove(reason.Trim(), staffId, at);
        // With none left, "no known allergies" has to be confirmed by someone, not assumed.
        if (!Allergies.Any(a => a.IsActive))
            AllergyStatus = AllergyStatus.Unknown;
        ModifiedAt = at;
        return Result.Success();
    }

    /// <summary>Someone asked, and the resident (or their record) says there are none.</summary>
    public Result RecordNoKnownAllergies(Guid staffId, DateTime at)
    {
        if (Allergies.Any(a => a.IsActive))
            return Result.Failure(Error.Conflict("Patients.HasRecordedAllergies",
                "This resident has allergies on file. Remove any entered in error first."));
        AllergyStatus = AllergyStatus.NoKnownAllergies;
        ModifiedAt = at;
        return Result.Success();
    }

    public Result<PatientAbsence> StartAbsence(AbsenceKind kind, DateTime startedAt, string? note, Guid staffId)
    {
        // Once they have left, they are not the home's to send to hospital or on leave.
        if (Residency != ResidencyStatus.Current)
            return Result.Failure<PatientAbsence>(NotResident);
        if (Absences.Any(a => a.IsOpen))
            return Result.Failure<PatientAbsence>(Error.Conflict("Patients.AlreadyAway",
                "This resident is already recorded as away. Record their return first."));
        if (Absences.Any(a => a.EndedAt > startedAt))
            return Result.Failure<PatientAbsence>(Error.Conflict("Patients.AbsenceOverlaps",
                "This overlaps an absence already recorded."));
        if (note?.Trim().Length > PatientAbsence.NoteMaxLength)
            return Result.Failure<PatientAbsence>($"The note can be at most {PatientAbsence.NoteMaxLength} characters.");

        var absence = PatientAbsence.Start(Id, kind, startedAt, string.IsNullOrWhiteSpace(note) ? null : note.Trim(), staffId);
        Absences.Add(absence);
        return Result.Success(absence);
    }

    public Result EndAbsence(DateTime endedAt, Guid staffId)
    {
        var open = Absences.FirstOrDefault(a => a.IsOpen);
        if (open is null)
            return Result.Failure(Error.Conflict("Patients.NotAway", "This resident is not recorded as away."));
        if (endedAt < open.StartedAt)
            return Result.Failure(Error.Validation("Patients.ReturnBeforeLeaving", "The return cannot be before the absence began."));

        open.End(endedAt, staffId);
        return Result.Success();
    }


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
