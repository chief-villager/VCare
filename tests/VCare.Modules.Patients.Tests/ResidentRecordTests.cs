using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using Xunit;

namespace VCare.Modules.Patients.Tests;

// The parts of the resident record a care home is asked for first: who they
// are to the NHS, who their GP is, where they sleep, what they weigh, who may
// decide for them, and whether they still live here.
public class ResidentRecordTests
{
    private static readonly Guid Manager = Guid.NewGuid();
    private static readonly DateTime Monday = new(2026, 11, 16, 10, 0, 0);

    private static Patient Resident() =>
        Patient.Register("Ada", "Obi", "Female", "Room 4", new DateOnly(1941, 3, 2),
            "Daughter", "07700 900123", "Ngozi Obi", null, Guid.NewGuid(), null).Value;

    [Theory]
    [InlineData("9434765919")]
    [InlineData("943 476 5919")]
    [InlineData("943-476-5919")]
    public void A_valid_nhs_number_is_stored_as_ten_digits(string written)
    {
        var patient = Resident();

        var result = patient.UpdateCareDetails(written, "Dr Okafor", "Riverside Surgery", "020 7946 0000", "12", new DateOnly(2026, 1, 5), Monday);

        Assert.True(result.IsSuccess);
        Assert.Equal("9434765919", patient.NhsNumber);
        Assert.Equal("12", patient.Room);
        Assert.Equal(new DateOnly(2026, 1, 5), patient.AdmittedOn);
    }

    [Theory]
    [InlineData("9434765918")]   // wrong check digit
    [InlineData("943476591")]    // nine digits
    [InlineData("94347659AB")]
    public void An_nhs_number_that_fails_its_check_digit_is_refused(string written)
    {
        var result = Resident().UpdateCareDetails(written, null, null, null, null, null, Monday);

        Assert.Equal("Patients.InvalidNhsNumber", result.Error.Code);
    }

    [Fact]
    public void Leaving_a_detail_empty_clears_it()
    {
        var patient = Resident();
        patient.UpdateCareDetails("9434765919", "Dr Okafor", null, null, "12", null, Monday);

        patient.UpdateCareDetails(null, "Dr Okafor", null, null, null, null, Monday);

        Assert.Null(patient.NhsNumber);
        Assert.Null(patient.Room);
    }

    [Fact]
    public void Weights_are_kept_as_a_history_and_the_latest_is_the_most_recently_measured()
    {
        var patient = Resident();

        patient.RecordWeight(61.4m, Monday.AddDays(-30), Manager, Monday);
        patient.RecordWeight(58.9m, Monday, Manager, Monday);
        patient.RecordWeight(60.2m, Monday.AddDays(-7), Manager, Monday);

        Assert.Equal(3, patient.Weights.Count);
        Assert.Equal(58.9m, patient.LatestWeight!.Kilograms);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(401)]
    public void An_impossible_weight_is_refused(decimal kilograms)
    {
        var result = Resident().RecordWeight(kilograms, Monday, Manager, Monday);

        Assert.Equal("Patients.InvalidWeight", result.Error.Code);
    }

    [Fact]
    public void A_discharged_resident_has_left_and_cannot_be_discharged_again()
    {
        var patient = Resident();

        var discharged = patient.Discharge(Monday, "Moved to her daughter's", Manager);
        var again = patient.Discharge(Monday.AddHours(1), null, Manager);

        Assert.True(discharged.IsSuccess);
        Assert.Equal(ResidencyStatus.Discharged, patient.Residency);
        Assert.Equal(Monday, patient.LeftAt);
        Assert.Equal("Patients.NotResident", again.Error.Code);
    }

    [Fact]
    public void Leaving_while_away_closes_the_absence_at_that_moment()
    {
        var patient = Resident();
        patient.StartAbsence(AbsenceKind.Hospital, Monday.AddDays(-2), null, Manager);

        patient.RecordDeath(Monday, Manager);

        Assert.Equal(ResidencyStatus.Deceased, patient.Residency);
        Assert.Equal(Monday, Assert.Single(patient.Absences).EndedAt);
    }

    [Fact]
    public void A_death_is_recorded_only_for_a_current_resident()
    {
        var patient = Resident();
        patient.Discharge(Monday, null, Manager);

        var result = patient.RecordDeath(Monday.AddDays(3), Manager);

        Assert.Equal("Patients.NotResident", result.Error.Code);
    }

    [Theory]
    [InlineData(ResidencyStatus.Discharged)]
    [InlineData(ResidencyStatus.Deceased)]
    public void A_resident_who_has_left_the_home_cannot_be_recorded_as_away(ResidencyStatus left)
    {
        // A hospital stay recorded after a death would have the missed-dose job write
        // "In hospital" against every dose after it.
        var patient = Resident();
        _ = left == ResidencyStatus.Discharged ? patient.Discharge(Monday, null, Manager) : patient.RecordDeath(Monday, Manager);

        var result = patient.StartAbsence(AbsenceKind.Hospital, Monday.AddDays(1), null, Manager);

        Assert.Equal("Patients.NotResident", result.Error.Code);
        Assert.Empty(patient.Absences);
    }

    [Fact]
    public void A_discharged_resident_can_be_readmitted_but_one_who_died_cannot()
    {
        var discharged = Resident();
        discharged.Discharge(Monday, null, Manager);
        var died = Resident();
        died.RecordDeath(Monday, Manager);

        var back = discharged.Readmit(Monday.AddDays(4), Manager, Monday.AddDays(4));
        var refused = died.Readmit(Monday.AddDays(4), Manager, Monday.AddDays(4));

        Assert.True(back.IsSuccess);
        Assert.Equal(ResidencyStatus.Current, discharged.Residency);
        Assert.Null(discharged.LeftAt);
        Assert.Equal(new DateOnly(2026, 11, 20), discharged.AdmittedOn);
        Assert.Equal("Patients.NotDischarged", refused.Error.Code);
    }

    [Fact]
    public void Readmission_keeps_the_time_away_on_the_record()
    {
        // Without it, the missed-dose job (which looks back a week) would count
        // every dose due while the resident was discharged as missed.
        var patient = Resident();
        patient.Discharge(Monday, null, Manager);

        patient.Readmit(Monday.AddDays(4), Manager, Monday.AddDays(4));

        var away = Assert.Single(patient.Absences);
        Assert.Equal(AbsenceKind.Discharged, away.Kind);
        Assert.Equal(Monday, away.StartedAt);
        Assert.Equal(Monday.AddDays(4), away.EndedAt);
    }

    [Fact]
    public void Readmission_ends_the_time_away_when_the_resident_came_back_not_when_it_was_entered()
    {
        // Back at 10:00 and entered at 15:00: a dose given at 12:00 must still be signable.
        var patient = Resident();
        patient.Discharge(Monday, null, Manager);
        var back = Monday.AddDays(4);

        patient.Readmit(back, Manager, back.AddHours(5));

        Assert.Equal(back, Assert.Single(patient.Absences).EndedAt);
        Assert.Equal(DateOnly.FromDateTime(back), patient.AdmittedOn);
    }

    [Fact]
    public void A_return_before_the_discharge_is_refused()
    {
        var patient = Resident();
        patient.Discharge(Monday, null, Manager);

        var result = patient.Readmit(Monday.AddHours(-1), Manager, Monday.AddDays(1));

        Assert.Equal("Patients.ReturnBeforeLeaving", result.Error.Code);
        Assert.Equal(ResidencyStatus.Discharged, patient.Residency);
        Assert.Empty(patient.Absences);
    }

    [Fact]
    public void An_attorney_is_kept_apart_from_next_of_kin_and_is_never_deleted()
    {
        var patient = Resident();

        var attorney = patient.RecordAttorney("Ngozi Obi", AttorneyKind.HealthAndWelfare, "07700 900123", null,
            new DateOnly(2024, 6, 1), Manager, Monday);
        var removed = patient.RemoveAttorney(attorney.Value.Id, "Registered in error", Manager, Monday.AddDays(1));

        Assert.True(removed.IsSuccess);
        var kept = Assert.Single(patient.Attorneys);
        Assert.False(kept.IsActive);
        Assert.Equal("Registered in error", kept.RemovalReason);
    }

    [Fact]
    public void Removing_an_attorney_needs_a_reason()
    {
        var patient = Resident();
        var attorney = patient.RecordAttorney("Ngozi Obi", AttorneyKind.PropertyAndFinancialAffairs, null, null, null, Manager, Monday);

        var result = patient.RemoveAttorney(attorney.Value.Id, " ", Manager, Monday);

        Assert.Equal("Patients.RemovalReasonRequired", result.Error.Code);
    }
}

// Discharge and death have their own endpoints; the absence form is for hospital
// stays and leave only.
public class AbsenceKindTests
{
    [Theory]
    [InlineData("Discharged")]
    [InlineData("Deceased")]
    public async Task Leaving_the_home_is_not_recorded_as_an_absence(string kind)
    {
        var (_, records) = AbsenceRecordTests.World();

        var result = await records.StartAbsenceAsync(Guid.NewGuid(), new StartAbsenceRequest(kind, null, null), CancellationToken.None);

        Assert.Equal("Patients.InvalidAbsenceKind", result.Error.Code);
    }
}

public class ResidentRecordServiceTests
{
    private static readonly Guid Manager = Guid.NewGuid();
    private static readonly Guid CareHome = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 19, 14, 0, 0);

    private sealed class Repository(Patient patient, bool nhsNumberInUse = false) : VCare.Modules.Patients.Application.Abstractions.IPatientRepository
    {
        public int Saves { get; private set; }

        /// <summary>The save fails; with <see cref="AnotherResidentTakesTheNhsNumber"/>, because the number was just taken.</summary>
        public bool SaveFails { get; set; }
        public bool AnotherResidentTakesTheNhsNumber { get; set; }
        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == patient.Id.Value ? patient : null);
        public Task<IReadOnlyList<Patient>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Patient>>([patient]);
        public Task AddAsync(Patient p, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (SaveFails)
            {
                nhsNumberInUse = AnotherResidentTakesTheNhsNumber;
                throw new Microsoft.EntityFrameworkCore.DbUpdateException("The save was refused.");
            }
            Saves++;
            return Task.CompletedTask;
        }
        public Task<bool> OwnsStorageKeyAsync(Guid patientId, string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<bool> NhsNumberInUseAsync(string nhsNumber, Guid exceptPatientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(nhsNumberInUse);
    }

    private sealed class Caller : ICurrentUser
    {
        public Guid UserId => Manager;
        public Guid CareHomeId => CareHome;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    private sealed class HomeClock : ICareHomeClock
    {
        public Task<DateTime> NowAsync(Guid careHomeId, CancellationToken cancellationToken) => Task.FromResult(Now);
    }

    private static (Patient Patient, Repository Repository, VCare.Modules.Patients.Application.Services.ResidentRecordService Service) World(bool nhsNumberInUse = false)
    {
        var patient = Patient.Register("Ada", "Obi", "Female", "Room 4", new DateOnly(1941, 3, 2),
            "Daughter", "07700 900123", "Ngozi Obi", null, CareHome, null).Value;
        var repository = new Repository(patient, nhsNumberInUse);
        return (patient, repository, new VCare.Modules.Patients.Application.Services.ResidentRecordService(repository, new Caller(), new HomeClock()));
    }

    [Fact]
    public async Task An_nhs_number_already_on_another_resident_in_the_home_is_refused()
    {
        var (patient, repository, service) = World(nhsNumberInUse: true);

        var result = await service.UpdateCareDetailsAsync(patient.Id.Value,
            new UpdateCareDetailsRequest("943 476 5919", null, null, null, null, null), CancellationToken.None);

        Assert.Equal("Patients.NhsNumberInUse", result.Error.Code);
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task An_nhs_number_taken_by_another_resident_while_saving_is_still_refused_as_in_use()
    {
        // Two admins enter one NHS number for two residents at once: both checks
        // pass, and the database's unique index refuses the second save.
        var (patient, repository, service) = World();
        repository.SaveFails = true;
        repository.AnotherResidentTakesTheNhsNumber = true;

        var result = await service.UpdateCareDetailsAsync(patient.Id.Value,
            new UpdateCareDetailsRequest("943 476 5919", null, null, null, null, null), CancellationToken.None);

        Assert.Equal("Patients.NhsNumberInUse", result.Error.Code);
    }

    [Fact]
    public async Task A_failed_save_is_not_blamed_on_an_nhs_number_nobody_else_has()
    {
        var (patient, repository, service) = World();
        repository.SaveFails = true;

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => service.UpdateCareDetailsAsync(patient.Id.Value,
            new UpdateCareDetailsRequest("943 476 5919", null, null, null, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task A_discharge_with_no_time_happens_now_in_the_home()
    {
        var (patient, _, service) = World();

        var result = await service.DischargeAsync(patient.Id.Value, new DischargeRequest(null, "Home with family"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, patient.LeftAt);
        Assert.Equal("Home with family", patient.DischargeDestination);
    }

    [Fact]
    public async Task A_death_cannot_be_recorded_in_advance()
    {
        var (patient, _, service) = World();

        var result = await service.RecordDeathAsync(patient.Id.Value, new RecordDeathRequest(Now.AddHours(2)), CancellationToken.None);

        Assert.Equal("Patients.InFuture", result.Error.Code);
        Assert.Equal(ResidencyStatus.Current, patient.Residency);
    }

    [Fact]
    public async Task A_readmission_with_no_time_happens_now_in_the_home()
    {
        var (patient, _, service) = World();
        patient.Discharge(Now.AddDays(-3), null, Manager);

        var result = await service.ReadmitAsync(patient.Id.Value, new ReadmitRequest(null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, Assert.Single(patient.Absences).EndedAt);
        Assert.Equal(DateOnly.FromDateTime(Now), patient.AdmittedOn);
    }

    [Fact]
    public async Task A_readmission_cannot_be_recorded_in_advance()
    {
        var (patient, _, service) = World();
        patient.Discharge(Now.AddDays(-3), null, Manager);

        var result = await service.ReadmitAsync(patient.Id.Value, new ReadmitRequest(Now.AddHours(2)), CancellationToken.None);

        Assert.Equal("Patients.InFuture", result.Error.Code);
        Assert.Equal(ResidencyStatus.Discharged, patient.Residency);
    }

    [Fact]
    public async Task A_weight_is_recorded_by_the_signed_in_member_of_staff()
    {
        var (patient, _, service) = World();

        var result = await service.RecordWeightAsync(patient.Id.Value, new RecordWeightRequest(58.94m, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var weight = Assert.Single(patient.Weights);
        Assert.Equal(58.9m, weight.Kilograms);
        Assert.Equal(Manager, weight.RecordedByStaffId);
        Assert.Equal(Now, weight.MeasuredAt);
    }

    [Fact]
    public async Task An_attorney_kind_must_be_one_of_the_two_lasting_powers()
    {
        var (patient, _, service) = World();

        var result = await service.RecordAttorneyAsync(patient.Id.Value,
            new RecordAttorneyRequest("Ngozi Obi", "NextOfKin", null, null, null), CancellationToken.None);

        Assert.Equal("Patients.InvalidAttorneyKind", result.Error.Code);
    }
}
