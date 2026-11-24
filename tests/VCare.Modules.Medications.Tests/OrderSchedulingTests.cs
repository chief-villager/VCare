using Medications.Application.Services;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Medications.Tests;

public class OrderSchedulingTests
{
    private static readonly DateOnly Day = new(2026, 11, 19); // a Thursday

    private static Result<MedicationOrder> Record(
        bool isPrn,
        params (string Dose, FrequencyType FType, List<TimeOnly> Times, int IntervalDays, DayOfWeekFlags DaysOfWeek,
            DateOnly? AnchorDate, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Sequence)[] schedules) =>
        MedicationOrder.Record(MedicationWorld.Resident, MedicationWorld.CareHome, "Amoxicillin 500mg",
            MedicationRouteEnum.ORAL, null, Day, null, "Dr Okafor", isPrn, isPrn ? "Pain" : null, null, null, false,
            OrderStatus.Active, schedules, Day.ToDateTime(new TimeOnly(7, 0)));

    [Fact]
    public void A_one_off_dose_is_due_once_on_its_day()
    {
        var order = Record(false,
            ("2 tablets", FrequencyType.OneOff, [new TimeOnly(14, 0)], 1, DayOfWeekFlags.None, null, Day.AddDays(1), null, 1)).Value;

        var slots = new ScheduleExpander().ExpandSchedule(order, Day, Day.AddDays(5)).ToList();

        var slot = Assert.Single(slots);
        Assert.Equal(new DateTime(2026, 11, 20, 14, 0, 0), slot.DueAt);
    }

    [Fact]
    public void A_one_off_dose_must_name_exactly_one_time()
    {
        var result = Record(false,
            ("2 tablets", FrequencyType.OneOff, [new TimeOnly(14, 0), new TimeOnly(18, 0)], 1, DayOfWeekFlags.None, null, Day, null, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void A_weekday_schedule_with_no_days_is_rejected()
    {
        // It would never fall due: the medicine would silently never be given.
        var result = Record(false,
            ("1 tablet", FrequencyType.SpecificWeekdays, [new TimeOnly(8, 0)], 1, DayOfWeekFlags.None, null, Day, null, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void A_regular_schedule_with_no_times_is_rejected()
    {
        var result = Record(false,
            ("1 tablet", FrequencyType.DailyAtTimes, [], 1, DayOfWeekFlags.None, null, Day, null, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void A_regular_order_with_no_schedule_is_rejected()
    {
        var result = Record(false);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void A_schedule_that_ends_before_it_starts_is_rejected()
    {
        var result = Record(false,
            ("1 tablet", FrequencyType.DailyAtTimes, [new TimeOnly(8, 0)], 1, DayOfWeekFlags.None, null, Day, Day.AddDays(-1), 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void A_prn_order_cannot_carry_a_timed_schedule()
    {
        // Timed doses on a PRN order are never expanded, so they would never be due.
        var result = Record(true,
            ("1 tablet", FrequencyType.DailyAtTimes, [new TimeOnly(8, 0)], 1, DayOfWeekFlags.None, null, Day, null, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task An_order_cannot_be_created_for_a_resident_who_is_not_in_the_home()
    {
        var world = new MedicationWorld(Day.ToDateTime(new TimeOnly(9, 0)));
        var stranger = Guid.Parse("dddddddd-0000-0000-0000-000000000009");

        var result = await world.Medications.CreateMedicationOrderAsync(stranger, new CreateMedicationOrderRequest(
            "Amoxicillin 500mg", "ORAL", null, Day, null, "Dr Okafor", false, null, null, null, false,
            [new ScheduleRequest("1 capsule", "DailyAtTimes", [new TimeOnly(8, 0)], 1, null, null, Day, null, 1)],
            "Active"), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(world.Orders.Orders);
    }

    [Fact]
    public async Task The_server_not_the_client_stamps_when_an_order_was_entered()
    {
        var now = Day.ToDateTime(new TimeOnly(9, 15));
        var world = new MedicationWorld(now);

        var result = await world.Medications.CreateMedicationOrderAsync(MedicationWorld.Resident, new CreateMedicationOrderRequest(
            "Amoxicillin 500mg", "ORAL", null, Day, null, "Dr Okafor", false, null, null, null, false,
            [new ScheduleRequest("1 capsule", "DailyAtTimes", [new TimeOnly(8, 0)], 1, null, null, Day, null, 1)],
            "Active"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(now, Assert.Single(world.Orders.Orders).CreatedAt);
    }
}
