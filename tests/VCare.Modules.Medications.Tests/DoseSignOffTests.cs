using Medications.Application.Services;
using Medications.Domain.Entities;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// Signing off a scheduled dose. The order: paracetamol at 08:00 and 20:00 every
// day, entered the evening before, so nothing about admission gets in the way.
public class DoseSignOffTests
{
    private static readonly DateOnly Day = new(2026, 11, 19);
    private static readonly DateTime Eight = Day.ToDateTime(new TimeOnly(8, 0));
    private static readonly DateTime Twenty = Day.ToDateTime(new TimeOnly(20, 0));

    private static (MedicationWorld World, MedicationOrder Order) RoundAt(DateTime now)
    {
        var world = new MedicationWorld(now);
        var order = world.AddDailyOrder(Day, Day.AddDays(-1).ToDateTime(new TimeOnly(18, 0)),
            new TimeOnly(8, 0), new TimeOnly(20, 0));
        return (world, order);
    }

    private static Task<Result<MedicationAdministrationResponse>> Sign(MedicationWorld world, MedicationOrder order,
        DateTime slot, string outcome = "Taken", DateTime? administeredAt = null, string? reason = null, Guid? patientId = null) =>
        world.Medications.RecordAministration(patientId ?? MedicationWorld.Resident, order.Id.Value, world.ScheduleIdOf(order),
            new RecordAdministrationRequest(slot, outcome, null, administeredAt, reason), MedicationWorld.Carer, CancellationToken.None);

    [Fact]
    public async Task An_evening_dose_cannot_be_signed_during_the_morning_round()
    {
        var (world, order) = RoundAt(Eight);

        var result = await Sign(world, order, Twenty);

        Assert.True(result.IsFailure);
        Assert.Equal("Medications.DoseNotYetDue", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task Tomorrows_dose_cannot_be_signed_today()
    {
        var (world, order) = RoundAt(Eight);

        var result = await Sign(world, order, Eight.AddDays(1));

        Assert.Equal("Medications.DoseNotYetDue", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_dose_can_be_signed_in_the_hour_before_it_is_due()
    {
        var (world, order) = RoundAt(Eight.AddMinutes(-40));

        var result = await Sign(world, order, Eight);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsLate);
    }

    [Fact]
    public async Task A_dose_signed_after_its_window_closes_needs_a_reason()
    {
        var (world, order) = RoundAt(Eight.AddMinutes(90));

        var result = await Sign(world, order, Eight);

        Assert.Equal("Medications.LateReasonRequired", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_late_dose_with_a_reason_is_recorded_as_given_and_flagged_late()
    {
        var (world, order) = RoundAt(Eight.AddMinutes(90));

        var result = await Sign(world, order, Eight, reason: "Resident was asleep; given on waking");

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(world.Administrations.Saved);
        Assert.Equal(OutcomeCode.TakenId, saved.OutcomeCodeId);
        Assert.True(saved.IsLate);
        Assert.Equal("Resident was asleep; given on waking", saved.Reason);
        Assert.Equal(Eight.AddMinutes(90), saved.AdministeredAt);
    }

    [Fact]
    public async Task A_carer_replaces_the_systems_missed_placeholder_with_what_actually_happened()
    {
        var (world, order) = RoundAt(Eight.AddHours(2));
        world.Administrations.Saved.Add(MedicationAdministration.RecordMissed(order, Eight,
            MedicationAdministration.SystemStaffId, "recorded automatically", Eight.AddMinutes(65)));

        var result = await Sign(world, order, Eight, administeredAt: Eight.AddMinutes(50),
            reason: "Given during the round, signed after handover");

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(world.Administrations.Saved);
        Assert.Equal(OutcomeCode.TakenId, saved.OutcomeCodeId);
        Assert.Equal(MedicationWorld.Carer, saved.AdministeredByStaffId);
        Assert.Equal(Eight.AddMinutes(50), saved.AdministeredAt);
        Assert.True(saved.IsLate);
    }

    [Fact]
    public async Task A_dose_cannot_be_recorded_twelve_hours_after_it_was_due()
    {
        var (world, order) = RoundAt(Eight.AddHours(12).AddMinutes(1));

        var result = await Sign(world, order, Eight, reason: "Forgot to sign");

        Assert.Equal("Medications.RecordingWindowClosed", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_carer_cannot_record_a_dose_as_missed()
    {
        var (world, order) = RoundAt(Eight);

        var result = await Sign(world, order, Eight, outcome: "Missed");

        Assert.Equal("Medications.MissedIsRecordedBySystem", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_dose_cannot_be_given_at_a_time_that_has_not_happened_yet()
    {
        var (world, order) = RoundAt(Eight);

        var result = await Sign(world, order, Eight, administeredAt: Eight.AddMinutes(30));

        Assert.Equal("Medications.AdministeredInFuture", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_dose_cannot_be_signed_through_another_residents_url()
    {
        var (world, order) = RoundAt(Eight);

        var result = await Sign(world, order, Eight, patientId: MedicationWorld.OtherResident);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task An_order_cannot_be_read_through_another_residents_url()
    {
        var (world, order) = RoundAt(Eight);

        var result = await world.Medications.GetMedicationOrder(MedicationWorld.OtherResident, order.Id.Value, CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
    }

    [Fact]
    public async Task An_order_cannot_be_stopped_through_another_residents_url()
    {
        var (world, order) = RoundAt(Eight);

        var result = await world.Medications.UpdateMedicationOrderStatusAsync(MedicationWorld.OtherResident,
            order.Id.Value, "Discontinued", CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal(global::Medications.Domain.Enum.OrderStatus.Active, order.Status);
    }
}
