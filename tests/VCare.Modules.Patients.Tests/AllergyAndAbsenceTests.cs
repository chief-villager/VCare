using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Application.Services;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Patients.Tests;

public class AllergyTests
{
    private static readonly Guid Nurse = Guid.NewGuid();
    private static readonly DateTime Monday = new(2026, 11, 16, 10, 0, 0);

    private static Patient Resident() =>
        Patient.Register("Ada", "Obi", "Female", "Room 4", new DateOnly(1941, 3, 2),
            "Daughter", "07700 900123", "Ngozi Obi", null, Guid.NewGuid(), null).Value;

    [Fact]
    public void A_new_resident_has_no_allergy_status_until_someone_records_one()
    {
        Assert.Equal(AllergyStatus.Unknown, Resident().AllergyStatus);
    }

    [Fact]
    public void Recording_an_allergy_marks_the_resident_as_having_allergies()
    {
        var patient = Resident();

        var allergy = patient.RecordAllergy("Penicillin", "Rash and swelling", AllergySeverity.Severe, Nurse, Monday);

        Assert.True(allergy.IsSuccess);
        Assert.Equal(AllergyStatus.HasAllergies, patient.AllergyStatus);
        Assert.Equal(Nurse, allergy.Value.RecordedByStaffId);
    }

    [Fact]
    public void The_same_substance_is_not_recorded_twice()
    {
        var patient = Resident();
        patient.RecordAllergy("Penicillin", null, AllergySeverity.Unknown, Nurse, Monday);

        var again = patient.RecordAllergy("penicillin ", null, AllergySeverity.Mild, Nurse, Monday);

        Assert.Equal("Patients.AllergyAlreadyRecorded", again.Error.Code);
    }

    [Fact]
    public void No_known_allergies_cannot_be_recorded_while_an_allergy_is_on_file()
    {
        var patient = Resident();
        patient.RecordAllergy("Latex", null, AllergySeverity.Moderate, Nurse, Monday);

        var result = patient.RecordNoKnownAllergies(Nurse, Monday);

        Assert.Equal("Patients.HasRecordedAllergies", result.Error.Code);
        Assert.Equal(AllergyStatus.HasAllergies, patient.AllergyStatus);
    }

    [Fact]
    public void Removing_the_last_allergy_needs_a_reason_and_leaves_the_status_to_be_confirmed_again()
    {
        var patient = Resident();
        var allergy = patient.RecordAllergy("Latex", null, AllergySeverity.Moderate, Nurse, Monday).Value;

        var unexplained = patient.RemoveAllergy(allergy.Id, " ", Nurse, Monday.AddHours(1));
        var removed = patient.RemoveAllergy(allergy.Id, "Entered on the wrong resident", Nurse, Monday.AddHours(1));

        Assert.True(unexplained.IsFailure);
        Assert.True(removed.IsSuccess);
        // Kept, marked removed: who said what, and when, stays on the record.
        Assert.Single(patient.Allergies);
        Assert.NotNull(patient.Allergies.Single().RemovedAt);
        Assert.Equal(AllergyStatus.Unknown, patient.AllergyStatus);
    }
}

public class AbsenceRecordTests
{
    private static readonly Guid Nurse = Guid.NewGuid();
    private static readonly Guid CareHome = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 19, 14, 0, 0);

    private static Patient Resident() =>
        Patient.Register("Ada", "Obi", "Female", "Room 4", new DateOnly(1941, 3, 2),
            "Daughter", "07700 900123", "Ngozi Obi", null, CareHome, null).Value;

    private sealed class Repository(Patient patient) : IPatientRepository
    {
        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == patient.Id.Value ? patient : null);
        public Task<IReadOnlyList<Patient>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Patient>>([patient]);
        public Task AddAsync(Patient p, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> OwnsStorageKeyAsync(Guid patientId, string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> NhsNumberInUseAsync(string nhsNumber, Guid exceptPatientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class Caller : ICurrentUser
    {
        public Guid UserId => Nurse;
        public Guid CareHomeId => CareHome;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    private sealed class HomeClock : ICareHomeClock
    {
        public Task<DateTime> NowAsync(Guid careHomeId, CancellationToken cancellationToken) => Task.FromResult(Now);
    }

    internal static (Patient Patient, ResidentRecordService Service) World()
    {
        var patient = Resident();
        return (patient, new ResidentRecordService(new Repository(patient), new Caller(), new HomeClock()));
    }

    [Fact]
    public async Task An_absence_with_no_start_time_starts_now_in_the_home()
    {
        var (patient, service) = World();

        var result = await service.StartAbsenceAsync(patient.Id.Value, new StartAbsenceRequest("Hospital", null, "Fall; taken to A&E"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var absence = Assert.Single(patient.Absences);
        Assert.Equal(AbsenceKind.Hospital, absence.Kind);
        Assert.Equal(Now, absence.StartedAt);
        Assert.Equal(Nurse, absence.RecordedByStaffId);
    }

    [Fact]
    public async Task An_absence_cannot_start_in_the_future()
    {
        var (patient, service) = World();

        var result = await service.StartAbsenceAsync(patient.Id.Value, new StartAbsenceRequest("Leave", Now.AddHours(3), null), CancellationToken.None);

        Assert.Equal("Patients.AbsenceInFuture", result.Error.Code);
    }

    [Fact]
    public async Task A_resident_already_away_cannot_leave_again_until_they_return()
    {
        var (patient, service) = World();
        await service.StartAbsenceAsync(patient.Id.Value, new StartAbsenceRequest("Hospital", Now.AddHours(-2), null), CancellationToken.None);

        var again = await service.StartAbsenceAsync(patient.Id.Value, new StartAbsenceRequest("Leave", null, null), CancellationToken.None);
        var back = await service.EndAbsenceAsync(patient.Id.Value, new EndAbsenceRequest(null), CancellationToken.None);

        Assert.Equal("Patients.AlreadyAway", again.Error.Code);
        Assert.True(back.IsSuccess);
        Assert.Equal(Now, patient.Absences.Single().EndedAt);
    }

    [Fact]
    public async Task A_return_cannot_be_before_the_absence_began()
    {
        var (patient, service) = World();
        await service.StartAbsenceAsync(patient.Id.Value, new StartAbsenceRequest("Hospital", Now.AddHours(-2), null), CancellationToken.None);

        var result = await service.EndAbsenceAsync(patient.Id.Value, new EndAbsenceRequest(Now.AddHours(-3)), CancellationToken.None);

        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
        Assert.Null(patient.Absences.Single().EndedAt);
    }

    [Fact]
    public async Task An_allergy_recorded_through_the_service_says_who_recorded_it()
    {
        var (patient, service) = World();

        var result = await service.RecordAllergyAsync(patient.Id.Value, new RecordAllergyRequest("Codeine", "Vomiting", "Moderate"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Nurse, patient.Allergies.Single().RecordedByStaffId);
        Assert.Equal(Now, patient.Allergies.Single().RecordedAt);
    }
}
