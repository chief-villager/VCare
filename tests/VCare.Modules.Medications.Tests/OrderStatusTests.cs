using Medications.Application.Services;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// Warfarin at 08:00 every day, entered on 1 November. The GP holds it on the 2nd
// at noon because the INR is high, and restarts it on the 4th at noon.
public class OrderStatusTests
{
    private static readonly DateOnly First = new(2026, 11, 1);
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    private static Task<Result> Change(MedicationWorld world, MedicationOrder order, string status, string? reason) =>
        world.Medications.UpdateMedicationOrderStatusAsync(MedicationWorld.Resident, order.Id.Value,
            new UpdateMedicationOrderStatusRequest(status, reason), MedicationWorld.Carer, CancellationToken.None);

    private static async Task<(MedicationWorld World, MedicationOrder Order)> HeldFromTheSecondToTheFourth()
    {
        var world = new MedicationWorld(At(1, 7));
        var order = world.AddDailyOrder(First, At(1, 7), new TimeOnly(8, 0));
        world.Clock.Now = At(2, 12);
        Assert.True((await Change(world, order, "Suspended", "INR 5.2, hold for two days")).IsSuccess);
        world.Clock.Now = At(4, 12);
        Assert.True((await Change(world, order, "Active", "INR 2.6, restart at the usual dose")).IsSuccess);
        return (world, order);
    }

    [Fact]
    public async Task A_suspended_order_can_be_resumed_and_each_change_says_who_and_why()
    {
        var (_, order) = await HeldFromTheSecondToTheFourth();

        Assert.Equal(OrderStatus.Active, order.Status);
        Assert.Collection(order.StatusHistory.OrderBy(c => c.ChangedAt),
            held =>
            {
                Assert.Equal((OrderStatus.Active, OrderStatus.Suspended), (held.From, held.To));
                Assert.Equal(At(2, 12), held.ChangedAt);
                Assert.Equal("INR 5.2, hold for two days", held.Reason);
                Assert.Equal(MedicationWorld.Carer, held.ChangedByStaffId);
            },
            restarted =>
            {
                Assert.Equal((OrderStatus.Suspended, OrderStatus.Active), (restarted.From, restarted.To));
                Assert.Equal(At(4, 12), restarted.ChangedAt);
            });
    }

    [Fact]
    public async Task A_status_change_needs_a_reason()
    {
        var world = new MedicationWorld(At(1, 7));
        var order = world.AddDailyOrder(First, At(1, 7), new TimeOnly(8, 0));

        var result = await Change(world, order, "Suspended", "  ");

        Assert.Equal("Medications.StatusReasonRequired", result.Error.Code);
        Assert.Equal(OrderStatus.Active, order.Status);
        Assert.Empty(order.StatusHistory);
    }

    [Fact]
    public async Task A_discontinued_order_cannot_be_restarted()
    {
        var world = new MedicationWorld(At(1, 7));
        var order = world.AddDailyOrder(First, At(1, 7), new TimeOnly(8, 0));
        Assert.True((await Change(world, order, "Discontinued", "Course complete")).IsSuccess);

        var result = await Change(world, order, "Active", "Started again");

        Assert.Equal("Medications.OrderDiscontinued", result.Error.Code);
        Assert.Equal(OrderStatus.Discontinued, order.Status);
    }

    [Fact]
    public async Task Doses_due_while_an_order_was_held_are_not_recorded_missed_after_it_restarts()
    {
        var (world, _) = await HeldFromTheSecondToTheFourth();
        world.Clock.Now = At(5, 10);

        await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        // The 1st and 2nd were due while it was live; the 3rd and the 4th's 08:00
        // were due while it was held; the 5th is due again.
        Assert.Equal([At(1, 8), At(2, 8), At(5, 8)],
            world.Administrations.Saved.Select(a => a.ScheduledFor!.Value).Order().ToList());
        Assert.All(world.Administrations.Saved, a => Assert.True(a.IsMissed));
    }

    [Fact]
    public async Task The_chart_leaves_out_doses_due_while_the_order_was_held_and_shows_the_changes()
    {
        var (world, _) = await HeldFromTheSecondToTheFourth();
        world.Clock.Now = At(5, 10);

        var row = (await world.Chart.BuildChart(MedicationWorld.Resident, 2026, 11, CancellationToken.None)).Value.Rows.Single();

        var due = row.Cells.Select(c => c.DueAt!.Value).ToList();
        Assert.Contains(At(2, 8), due);
        Assert.DoesNotContain(At(3, 8), due);
        Assert.DoesNotContain(At(4, 8), due);
        Assert.Contains(At(5, 8), due);
        Assert.Equal(2, row.StatusChanges.Count);
    }

    [Fact]
    public async Task A_dose_due_while_the_order_was_held_cannot_be_signed_after_it_restarts()
    {
        var (world, order) = await HeldFromTheSecondToTheFourth();
        world.Clock.Now = At(4, 12, 30);

        var result = await world.Medications.RecordAministration(MedicationWorld.Resident, order.Id.Value,
            world.ScheduleIdOf(order), new RecordAdministrationRequest(At(4, 8), "Taken", null, null, "Given at 08:00"),
            MedicationWorld.Carer, CancellationToken.None);

        Assert.Equal("Medications.OrderNotActive", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }
}
