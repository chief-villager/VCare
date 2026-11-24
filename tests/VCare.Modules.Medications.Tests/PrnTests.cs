using Medications.Application.Services;
using Medications.Domain.Entities;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// As-required paracetamol: at least four hours apart, at most two doses in any
// 24 hours (a deliberately small limit so the arithmetic is easy to check).
public class PrnTests
{
    private static readonly DateOnly Day = new(2026, 11, 19);
    private static DateTime At(int hour, int minute = 0, int dayOffset = 0) =>
        Day.AddDays(dayOffset).ToDateTime(new TimeOnly(hour, minute));

    private static (MedicationWorld World, MedicationOrder Order) PrnAt(DateTime now, int? minInterval = 240, int? maxPer24h = 2)
    {
        var world = new MedicationWorld(now);
        var order = world.AddPrnOrder(Day.AddDays(-7), At(9, 0, -7), minInterval, maxPer24h);
        return (world, order);
    }

    private static Task<Result<MedicationAdministrationResponse>> Give(MedicationWorld world, MedicationOrder order,
        string outcome = "Taken", string dose = "2 tablets", string reason = "Pain in left hip") =>
        world.Medications.RecordPrnAdministration(MedicationWorld.Resident, order.Id.Value,
            new RecordPrnAdministrationRequest(outcome, dose, reason), MedicationWorld.Carer, CancellationToken.None);

    private static async Task GiveAt(MedicationWorld world, MedicationOrder order, DateTime when, string outcome = "Taken")
    {
        var now = world.Clock.Now;
        world.Clock.Now = when;
        var result = await Give(world, order, outcome);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.ToString() : null);
        world.Clock.Now = now;
    }

    [Fact]
    public async Task A_prn_dose_is_recorded_with_the_dose_given_and_why()
    {
        var (world, order) = PrnAt(At(10));

        var result = await Give(world, order);

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(world.Administrations.Saved);
        Assert.Null(saved.ScheduledFor);
        Assert.Equal(At(10), saved.AdministeredAt);
        Assert.Equal("2 tablets", saved.DoseGiven);
        Assert.Equal("Pain in left hip", saved.Reason);
        Assert.Equal(OutcomeCode.TakenId, saved.OutcomeCodeId);
    }

    [Fact]
    public async Task A_prn_dose_inside_the_minimum_interval_is_refused()
    {
        var (world, order) = PrnAt(At(11, 59));
        await GiveAt(world, order, At(8));

        var result = await Give(world, order);

        Assert.Equal("Medications.PrnTooSoon", result.Error.Code);
        Assert.Single(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_prn_dose_after_the_minimum_interval_is_allowed()
    {
        var (world, order) = PrnAt(At(12, 0));
        await GiveAt(world, order, At(8));

        var result = await Give(world, order);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task A_third_dose_inside_twenty_four_hours_is_refused()
    {
        var (world, order) = PrnAt(At(22));
        await GiveAt(world, order, At(10));
        await GiveAt(world, order, At(16));

        var result = await Give(world, order);

        Assert.Equal("Medications.PrnDailyLimitReached", result.Error.Code);
        Assert.Equal(2, world.Administrations.Saved.Count);
    }

    [Fact]
    public async Task The_daily_limit_rolls_rather_than_resetting_at_midnight()
    {
        // 10:00 and 16:00 yesterday: at 01:00 today both are still inside the last
        // 24 hours, even though the calendar day has changed.
        var (world, order) = PrnAt(At(1, 0, dayOffset: 1));
        await GiveAt(world, order, At(10));
        await GiveAt(world, order, At(16));

        var result = await Give(world, order);

        Assert.Equal("Medications.PrnDailyLimitReached", result.Error.Code);
    }

    [Fact]
    public async Task A_dose_is_allowed_again_once_the_earliest_one_is_24_hours_old()
    {
        var (world, order) = PrnAt(At(10, 1, dayOffset: 1));
        await GiveAt(world, order, At(10));
        await GiveAt(world, order, At(16));

        var result = await Give(world, order);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task A_refused_offer_does_not_count_towards_the_interval()
    {
        var (world, order) = PrnAt(At(9));
        await GiveAt(world, order, At(8), outcome: "Refused");

        var result = await Give(world, order);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task A_prn_dose_needs_the_dose_given()
    {
        var (world, order) = PrnAt(At(10));

        var result = await Give(world, order, dose: " ");

        Assert.Equal("Medications.PrnDoseRequired", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_prn_dose_needs_a_reason()
    {
        var (world, order) = PrnAt(At(10));

        var result = await Give(world, order, reason: "");

        Assert.Equal("Medications.PrnReasonRequired", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_regular_order_cannot_be_given_as_prn()
    {
        var world = new MedicationWorld(At(10));
        var order = world.AddDailyOrder(Day, At(7), new TimeOnly(8, 0));

        var result = await Give(world, order);

        Assert.Equal("Medications.NotAPrnOrder", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_prn_dose_cannot_be_recorded_through_another_residents_url()
    {
        var (world, order) = PrnAt(At(10));

        var result = await world.Medications.RecordPrnAdministration(MedicationWorld.OtherResident, order.Id.Value,
            new RecordPrnAdministrationRequest("Taken", "1 tablet", "Headache"), MedicationWorld.Carer, CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(world.Administrations.Saved);
    }
}
